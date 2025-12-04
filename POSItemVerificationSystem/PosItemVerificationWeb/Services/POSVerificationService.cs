using Microsoft.Data.SqlClient;
using PosItemVerificationWeb.Models; 
using System.Data; 
using PosItemVerificationWeb.Services; 
using System.Text.Json;
namespace PosItemVerificationWeb.Services
{
     
 
        public class POSVerificationService : IPOSVerificationService
        {
            private readonly string _posConnectionString;
            private readonly ILogger<POSVerificationService> _logger;

            public POSVerificationService(IConfiguration configuration, ILogger<POSVerificationService> logger)
            {
                _posConnectionString = configuration.GetConnectionString("POSConnection")
                    ?? throw new InvalidOperationException("Connection string 'POSConnection' not found.");
                _logger = logger;
            }

            public async Task<List<POSVerificationGroup>> GetVerificationGroupsAsync()
            {
                var items = await GetPOSVerificationItemsAsync();
                return GroupItems(items);
            }

            public async Task<POSVerificationSummary> GetSummaryAsync()
            {
                var groups = await GetVerificationGroupsAsync();

                return new POSVerificationSummary
                {
                    TotalGroups = groups.Count,
                    UpdateActions = groups.Count(g => g.SelectedAction == "Update"),
                    CreateNewActions = groups.Count(g => g.SelectedAction == "Create New"),
                    ItemsWithPOSDeleted = groups.Count(g => g.HasPOSDeleted)
                };
            }

            public async Task<ActionUpdateResult> SubmitActionsAsyncOld(SubmitActionsRequest request)
            {
                var result = new ActionUpdateResult();

                try
                {
                    // Get POSItemKeys for the audit keys first
                    await EnrichActionsWithPOSItemKeysAsync(request.Actions);

                    // Use JSON approach if SQL Server 2016+ is available
                    if (await SupportsJsonAsync())
                    {
                        result = await SubmitActionsViaBulkJsonAsync(request.Actions);
                    }
                    else
                    {
                        // Fallback to individual updates for older SQL Server versions
                        result = await SubmitActionsIndividuallyAsync(request.Actions);
                    }

                    if (result.Success && result.TotalItemsUpdated > 0)
                    {
                        _logger.LogInformation($"Successfully updated {result.TotalItemsUpdated} POS items for user {request.SubmittedBy}");

                        // Send email notification
                        await SendEmailNotificationAsync(request, result);
                    }
                    else if (result.TotalItemsUpdated == 0)
                    {
                        result.Success = false;
                        result.Message = "No items were updated. They may have already been processed.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error submitting actions for user {User}", request.SubmittedBy);
                    result.Success = false;
                    result.Message = $"Database error: {ex.Message}";
                    result.Errors.Add(ex.ToString());
                }

                return result;
            }

            private async Task EnrichActionsWithPOSItemKeysAsync(List<ActionSelection> actions)
            {
                using var connection = new SqlConnection(_posConnectionString);
                await connection.OpenAsync();

                foreach (var action in actions)
                {
                    using var command = new SqlCommand(
                        "SELECT POSItemKey FROM Fact.POSItem WHERE RAWID = @AuditKey",
                        connection);
                    command.Parameters.AddWithValue("@AuditKey", action.AuditKey);

                    var result = await command.ExecuteScalarAsync();
                    action.POSItemKey = result?.ToString() ?? "";
                }
            }

            private async Task<bool> SupportsJsonAsync()
            {
                try
                {
                    using var connection = new SqlConnection(_posConnectionString);
                    using var command = new SqlCommand("SELECT ISJSON('[]')", connection);
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) == 1;
                }
                catch
                {
                    return false; // JSON not supported
                }
            }

            private async Task<ActionUpdateResult> SubmitActionsViaBulkJsonAsync(List<ActionSelection> actions)
            {
                var result = new ActionUpdateResult();

                // Convert actions to JSON
                var actionsJson = JsonSerializer.Serialize(actions.Select(a => new {
                    auditKey = a.AuditKey,
                    action = a.Action,
                    posItemKey = a.POSItemKey
                }));

                using var connection = new SqlConnection(_posConnectionString);
                using var command = new SqlCommand("sp_POSItemActions_Update", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@ActionUpdates", actionsJson);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    result.Success = true;
                    result.TotalItemsUpdated = Convert.ToInt32(reader["TotalItemsUpdated"]);
                    result.UpdateActions = Convert.ToInt32(reader["UpdateActions"]);
                    result.CreateActions = Convert.ToInt32(reader["CreateActions"]);
                    result.Message = $"Successfully updated {result.TotalItemsUpdated} items";
                }

            /*
             Exception: 
System.IndexOutOfRangeException: TotalItemsUpdated
at Microsoft.Data.ProviderBase.FieldNameLookup.GetOrdinal(String fieldName)
at Microsoft.Data.SqlClient.SqlDataReader.GetOrdinal(String name)
at Microsoft.Data.SqlClient.SqlDataReader.get_Item(String name)
at PosItemVerificationWeb.Services.POSVerificationService.SubmitActionsViaBulkJsonAsync(List`1 actions) in C:\Users\daniels\source\repos\POSItemVerificationSystem\PosItemVerificationWeb\Services\POSVerificationService.cs:line 143
at PosItemVerificationWeb.Services.POSVerificationService.SubmitActionsAsync(SubmitActionsRequest request) in C:\Users\daniels\source\repos\POSItemVerificationSystem\PosItemVerificationWeb\Services\POSVerificationService.cs:line 324


             */

            return result;
            }

            private async Task<ActionUpdateResult> SubmitActionsIndividuallyAsync(List<ActionSelection> actions)
            {
                var result = new ActionUpdateResult();
                int totalUpdated = 0;
                int updateCount = 0;
                int createCount = 0;

                using var connection = new SqlConnection(_posConnectionString);
                await connection.OpenAsync();

                foreach (var action in actions)
                {
                    try
                    {
                        using var command = new SqlCommand("sp_POSItemActions_UpdateSingle", connection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        command.Parameters.AddWithValue("@AuditKey", action.AuditKey);
                        command.Parameters.AddWithValue("@Action", action.Action);

                        var rowsAffected = Convert.ToInt32(await command.ExecuteScalarAsync());

                        if (rowsAffected > 0)
                        {
                            totalUpdated += rowsAffected;
                            if (action.Action == "Update") updateCount++;
                            else if (action.Action == "Create New") createCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to update action for AuditKey {action.AuditKey}: {ex.Message}");
                        result.Errors.Add($"Failed to update {action.AuditKey}: {ex.Message}");
                    }
                }

                result.Success = totalUpdated > 0;
                result.TotalItemsUpdated = totalUpdated;
                result.UpdateActions = updateCount;
                result.CreateActions = createCount;
                result.Message = result.Success
                    ? $"Successfully updated {totalUpdated} items"
                    : "No items were updated";

                return result;
            }

            public async Task<bool> SendEmailNotificationAsync(SubmitActionsRequest request, ActionUpdateResult result)
            {
                try
                {
                    // Here you would integrate with your email service
                    // For now, just log the details that would be emailed

                    var emailContent = $@"
POS Item Verification Actions Submitted

Submitted by: {request.SubmittedBy}
Submission time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Summary:
- Total items processed: {result.TotalItemsUpdated}
- Items set to Update: {result.UpdateActions}  
- Items set to Create New: {result.CreateActions}

Actions taken:
{string.Join("\n", request.Actions.Select(a => $"- {a.Brand} {a.POSCode}: {a.Action}"))}

Please review and process these changes in the master sync system.
                ";

                    _logger.LogInformation($"Email notification prepared for {request.EmailAddress}: {emailContent}");

                    // TODO: Implement actual email sending
                    // await _emailService.SendAsync(request.EmailAddress, "POS Verification Actions Submitted", emailContent);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification");
                    return false;
                }
            }

            // ... (rest of existing methods remain the same)
            private async Task<List<POSVerificationItem>> GetPOSVerificationItemsAsync()
            {
                var items = new List<POSVerificationItem>();

                using var connection = new SqlConnection(_posConnectionString);
                using var command = new SqlCommand("sp_POSItemActions", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    if (string.IsNullOrEmpty(reader["DataType"].ToString()) &&
                        string.IsNullOrEmpty(reader["POSCode"].ToString()))
                        continue;

                    var item = new POSVerificationItem
                    {
                        DataType = reader["DataType"].ToString(),
                        POSDataSetBrandKey = reader["POSDataSetBrandKey"].ToString(),
                        EffectiveFrom = reader["EffectiveFrom"].ToString(),
                        PointOfSale = reader["PointOfSale"].ToString(),
                        POSDataSet = reader["POSDataSet"].ToString(),
                        POSDataSetVersion = reader["POSDataSetVersion"].ToString(),
                        POSCode = reader["POSCode"].ToString(),
                        POSName = reader["POSName"].ToString(),
                        POSDTAB = reader["POSDTAB"].ToString(),
                        POSPrice = reader["POSPrice"].ToString(),
                        POSItemKey = reader["POSItemKey"].ToString(),
                        POSItemStatus = reader["POSItemStatus"].ToString(),
                        SyncAction = reader["SyncAction"].ToString(),
                        SelectAction = reader["SelectAction"].ToString(),
                        CreatedOn = reader["CreatedOn"].ToString(),
                        LastRecordedSale = reader["LastRecordedSale"].ToString(),
                        AuditKey = reader["AuditKey"].ToString(),
                        AuditOrder = Convert.ToInt32(reader["AuditOrder"])
                    };

                    items.Add(item);
                }

                return items;
            }

            private List<POSVerificationGroup> GroupItems(List<POSVerificationItem> items)
            {
                return items
                    .Where(i => !string.IsNullOrEmpty(i.DataType) && i.DataType != "Create New")
                    .GroupBy(i => i.AuditKey)
                    .Select(g => new POSVerificationGroup
                    {
                        AuditKey = g.Key,
                        Brand = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.POSDataSetBrandKey))?.POSDataSetBrandKey ?? "",
                        POSCode = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.POSCode))?.POSCode ?? "",
                        POSItemKey = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.POSItemKey))?.POSItemKey ?? "",
                        NewUpdate = g.FirstOrDefault(i => i.DataType == "New Update"),
                        Current = g.FirstOrDefault(i => i.DataType == "Current"),
                        Others = g.Where(i => i.DataType == "Other").ToList(),
                        SelectedAction = "Update"
                    })
                    .Where(g => g.NewUpdate != null)
                    .ToList();
            }


        public async Task<ActionUpdateResult> SubmitActionsAsync(SubmitActionsRequest request)
        {
            var sessionId = Guid.NewGuid();
            var result = new ActionUpdateResult();

            try
            {
                // Get POSItemKeys for the audit keys first
                await EnrichActionsWithPOSItemKeysAsync(request.Actions);

                // Log current state before making changes
                await LogCurrentStateAsync(request.Actions, sessionId, request.SubmittedBy);

                // Use JSON approach if SQL Server 2016+ is available
                if (await SupportsJsonAsync())
                {
                    result = await SubmitActionsViaBulkJsonAsync(request.Actions);
                }
                else
                {
                    // Fallback to individual updates for older SQL Server versions
                    result = await SubmitActionsIndividuallyAsync(request.Actions);
                }

                if (result.Success && result.TotalItemsUpdated > 0)
                {
                    result.SessionId = sessionId; // Add session ID to result for rollback
                    _logger.LogInformation($"Session {sessionId}: Successfully updated {result.TotalItemsUpdated} POS items for user {request.SubmittedBy}");

                    // Send email notification
                    await SendEmailNotificationAsync(request, result);
                }
                else if (result.TotalItemsUpdated == 0)
                {
                    result.Success = false;
                    result.Message = "No items were updated. They may have already been processed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting actions for user {User}", request.SubmittedBy);
                result.Success = false;
                result.Message = $"Database error: {ex.Message}";
                result.Errors.Add(ex.ToString());
            }

            return result;
        }

        // Add this new method to log the current state
        private async Task LogCurrentStateAsync(List<ActionSelection> actions, Guid sessionId, string submittedBy)
        {
            using var connection = new SqlConnection(_posConnectionString);
            await connection.OpenAsync();

            foreach (var action in actions)
            {
                using var command = new SqlCommand(@"
            INSERT INTO POSItemActionAudit 
            (SessionID, AuditKey, POSItemKey, POSCode, Brand, OldSyncAction, NewSyncAction, OldPOSItemKey, NewPOSItemKey, SubmittedBy)
            SELECT 
                @SessionID, f.RAWID, f.POSItemKey, f.POSCode, f.POSDataSetBrandKey,
                f.SyncFromSourceAction, @NewAction,
                f.POSItemKey, 
                CASE WHEN @NewAction = 'Create' THEN NULL ELSE f.POSItemKey END,
                @SubmittedBy
            FROM Fact.POSItem f
            WHERE f.RAWID = @AuditKey", connection);

                command.Parameters.AddWithValue("@SessionID", sessionId);
                command.Parameters.AddWithValue("@AuditKey", action.AuditKey);
                command.Parameters.AddWithValue("@NewAction", action.Action == "Create New" ? "Create" : "Update");
                command.Parameters.AddWithValue("@SubmittedBy", submittedBy);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Add rollback method
        public async Task<bool> RollbackActionsAsync(Guid sessionId, string requestedBy)
        {
            try
            {
                using var connection = new SqlConnection(_posConnectionString);
                using var command = new SqlCommand("sp_POSItemActions_Rollback", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@SessionID", sessionId);

                await connection.OpenAsync();
                var itemsRolledBack = Convert.ToInt32(await command.ExecuteScalarAsync());

                _logger.LogInformation($"Session {sessionId}: Rolled back {itemsRolledBack} items by {requestedBy}");

                return itemsRolledBack > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rolling back session {sessionId}");
                return false;
            }
        }
    }
    }


    //public class POSVerificationService : IPOSVerificationService
    //{
    //    private readonly string _connectionString;
    //    private readonly ILogger<POSVerificationService> _logger;

    //    public POSVerificationService(IConfiguration configuration, ILogger<POSVerificationService> logger)
    //    {
    //        _connectionString = configuration.GetConnectionString("DefaultConnection2");
    //        _logger = logger;
    //    }

    //    public async Task<List<POSVerificationGroup>> GetVerificationGroupsAsync()
    //    {
    //        var items = await GetPOSVerificationItemsAsync();
    //        return GroupItems(items);
    //    }

    //    public async Task<POSVerificationSummary> GetSummaryAsync()
    //    {
    //        var groups = await GetVerificationGroupsAsync();

    //        return new POSVerificationSummary
    //        {
    //            TotalGroups = groups.Count,
    //            UpdateActions = groups.Count(g => g.SelectedAction == "Update"),
    //            CreateNewActions = groups.Count(g => g.SelectedAction == "Create New"),
    //            ItemsWithPOSDeleted = groups.Count(g => g.HasPOSDeleted)
    //        };
    //    }

    //    public async Task<bool> SubmitActionsAsync(SubmitActionsRequest request)
    //    {
    //        try
    //        {
    //            // Here you would typically:
    //            // 1. Update the database with selected actions
    //            // 2. Send email to specified address
    //            // 3. Log the submission

    //            _logger.LogInformation($"Submitted {request.Actions.Count} actions to {request.EmailAddress}");

    //            // For now, just return success
    //            // In real implementation, you'd update the database and send email
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error submitting actions");
    //            return false;
    //        }
    //    }

    //    private async Task<List<POSVerificationItem>> GetPOSVerificationItemsAsync()
    //    {
    //        var items = new List<POSVerificationItem>();

    //        using var connection = new SqlConnection(_connectionString);
    //        using var command = new SqlCommand("sp_POSItemActions", connection)
    //        {
    //            CommandType = CommandType.StoredProcedure
    //        };

    //        await connection.OpenAsync();
    //        using var reader = await command.ExecuteReaderAsync();

    //        while (await reader.ReadAsync())
    //        {
    //            // Skip empty separator rows
    //            if (string.IsNullOrEmpty(reader["DataType"].ToString()) &&
    //                string.IsNullOrEmpty(reader["POSCode"].ToString()))
    //                continue;

    //            var item = new POSVerificationItem
    //            {
    //                DataType = reader["DataType"].ToString(),
    //                POSDataSetBrandKey = reader["POSDataSetBrandKey"].ToString(),
    //                EffectiveFrom = reader["EffectiveFrom"].ToString(),
    //                PointOfSale = reader["PointOfSale"].ToString(),
    //                POSDataSet = reader["POSDataSet"].ToString(),
    //                POSDataSetVersion = reader["POSDataSetVersion"].ToString(),
    //                POSCode = reader["POSCode"].ToString(),
    //                POSName = reader["POSName"].ToString(),
    //                POSDTAB = reader["POSDTAB"].ToString(),
    //                POSPrice = reader["POSPrice"].ToString(),
    //                POSItemKey = reader["POSItemKey"].ToString(),
    //                POSItemStatus = reader["POSItemStatus"].ToString(),
    //                SyncAction = reader["SyncAction"].ToString(),
    //                SelectAction = reader["SelectAction"].ToString(),
    //                CreatedOn = reader["CreatedOn"].ToString(),
    //                LastRecordedSale = reader["LastRecordedSale"].ToString(),
    //                AuditKey = reader["AuditKey"].ToString(),
    //                AuditOrder = Convert.ToInt32(reader["AuditOrder"])
    //            };

    //            items.Add(item);
    //        }

    //        return items;
    //    }

    //    private List<POSVerificationGroup> GroupItems(List<POSVerificationItem> items)
    //    {
    //        return items
    //            .Where(i => !string.IsNullOrEmpty(i.DataType) && i.DataType != "Create New") // Exclude action rows
    //            .GroupBy(i => i.AuditKey)
    //            .Select(g => new POSVerificationGroup
    //            {
    //                AuditKey = g.Key,
    //                Brand = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.POSDataSetBrandKey))?.POSDataSetBrandKey ?? "",
    //                POSCode = g.FirstOrDefault(x => !string.IsNullOrEmpty(x.POSCode))?.POSCode ?? "",
    //                NewUpdate = g.FirstOrDefault(i => i.DataType == "New Update"),
    //                Current = g.FirstOrDefault(i => i.DataType == "Current"),
    //                Others = g.Where(i => i.DataType == "Other").ToList(),
    //                SelectedAction = "Update" // Default action
    //            })
    //            .Where(g => g.NewUpdate != null) // Only groups with new updates
    //            .ToList();
    //    }
    //}


