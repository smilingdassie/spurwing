using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PosItemVerificationWeb.Models;

namespace PosItemVerificationWeb.Repositories
{
    // Repositories/DashboardRepository.cs


    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<SalesForceMetric>> GetSalesForceMetricsAsync()
        {
            const string query = @"
            SELECT 
                Processed, 
                ProcessedBy, 
                TableName, 
                COUNT(*) AS NumberOfRecords
            FROM MasterSync.IntoSalesForce.MasterDataUpdated WITH(NOLOCK)
            GROUP BY Processed, ProcessedBy, TableName";

            using var connection = new SqlConnection(_connectionString);
            var results = await connection.QueryAsync<SalesForceMetric>(query);
            return results.ToList();
        }

        public async Task<List<CrmMetric>> GetCrmMetricsAsync()
        {
            const string query = @"
            SELECT 
                Processed, 
                ProcessedBy, 
                SourceSystem, 
                COUNT(*) AS NumberOfRecords
            FROM MasterSync.IntoCRM.LoyaltyCardUpdated WITH(NOLOCK)
            GROUP BY Processed, ProcessedBy, SourceSystem";

            using var connection = new SqlConnection(_connectionString);
            var results = await connection.QueryAsync<CrmMetric>(query);
            return results.ToList();
        }

        public async Task<List<DashboardSnapshot>> GetMetricsHistoryAsync(DateTime since)
        {
            const string query = @"
            SELECT 
                Id,
                Timestamp,
                SalesForceProcessed,
                SalesForcePending,
                CrmProcessed,
                CrmPending
            FROM MasterData.DashboardMetricsHistory WITH(NOLOCK)
            WHERE Timestamp >= @Since
            ORDER BY Timestamp ASC";

            using var connection = new SqlConnection(_connectionString);
            var results = await connection.QueryAsync<DashboardSnapshot>(query, new { Since = since });
            return results.ToList();
        }

        public async Task SaveSnapshotAsync(DashboardSnapshot snapshot)
        {
            const string query = @"
            INSERT INTO MasterData.DashboardMetricsHistory 
            (Timestamp, SalesForceProcessed, SalesForcePending, CrmProcessed, CrmPending)
            VALUES 
            (@Timestamp, @SalesForceProcessed, @SalesForcePending, @CrmProcessed, @CrmPending)";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query, snapshot);
        }

        public async Task DeleteSnapshotsOlderThanAsync(DateTime cutoffDate)
        {
            const string query = @"
            DELETE FROM MasterData.DashboardMetricsHistory
            WHERE Timestamp < @CutoffDate";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(query, new { CutoffDate = cutoffDate });
        }
    }


}
