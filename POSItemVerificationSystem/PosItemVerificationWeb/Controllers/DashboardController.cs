using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosItemVerificationWeb.Models;
using PosItemVerificationWeb.Repositories;
using System.Data;
using System.Text;
using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Globalization;
using Microsoft.Extensions.Options;
using PosItemVerificationWeb.Helpers;

namespace PosItemVerificationWeb.Controllers
{
    // Models/DashboardMetrics.cs

    [Authorize]
    public class DashboardController : Controller
    {


        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly AllowedUsersConfig _allowed;

        


        public DashboardController(IConfiguration configuration, IOptions<AllowedUsersConfig> allowed)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("RestaurantOpeningConnection");
            _allowed = allowed.Value;
        }

        public static string LoadSql(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


        public ActionResult Report()
        {
            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }

            var model = new SqlQueryResult();
            // Populate dropdown options
            model.ServerDatabases = _configuration.GetSection("ConnectionStrings")
            .GetChildren()
            .Select(x => x.Key)
            .ToList();



            return View(model);

        }
        public ActionResult Index()
        {

            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }



            var model = new SqlQueryResult();

            model.AvailableScripts = GetDashboardScripts()
     .Select(r => new ScriptItem
     {
         ScriptName = Path.GetFileName(r),  // used for loading
         DisplayName = CultureInfo.CurrentCulture.TextInfo
             .ToTitleCase(Path.GetFileNameWithoutExtension(r).Replace("_", " "))
     })
     .ToList();
            model.ServerConnections = _configuration.GetSection("ConnectionStrings")
    .GetChildren()
    .Select(x => x.Key)
    .ToList();


            return View(model);
        }




        [HttpPost]
        public JsonResult Report(SqlQueryResult model)
        {
            if (string.IsNullOrWhiteSpace(model.SelectedServerDatabase))
            {
                return Json(new { error = "Please select a server-database." });
            }

            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return Json(new { error = "Please enter a SQL query." });
            }

            try
            {
                string connectionString = _configuration.GetConnectionString(model.SelectedServerDatabase);

                if (string.IsNullOrEmpty(connectionString))
                {
                    return Json(new { error = $"Connection string '{model.SelectedServerDatabase}' not found." });
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(model.Query, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Convert to Tabulator format
                    var columns = dt.Columns.Cast<DataColumn>()
                        .Select(col => new
                        {
                            title = col.ColumnName,
                            field = col.ColumnName
                        })
                        .ToList();

                    var rows = dt.AsEnumerable()
                        .Select(row => dt.Columns.Cast<DataColumn>()
                            .ToDictionary(
                                col => col.ColumnName,
                                col => row[col] == DBNull.Value ? null : row[col]
                            ))
                        .ToList();

                    return Json(new { columns, rows });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private List<string> GetDashboardScripts()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "App_Data", "SqlScripts", "Dashboard");

            if (!Directory.Exists(folder))
                return new List<string>();

            return Directory.GetFiles(folder, "*.sql")
                            .Select(Path.GetFileName)
                            .ToList();
        }

        private string LoadSqlFromDisk(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "App_Data", "SqlScripts", "Dashboard", fileName);

            if (!System.IO.File.Exists(path))
                throw new Exception("SQL script not found: " + path);

            return System.IO.File.ReadAllText(path);
        }


        [HttpPost]
        public JsonResult IndexOld(string ScriptName, string ServerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ServerName))
                    return Json(new { error = "Select a server to run the script on." });

                string connString = _configuration.GetConnectionString(ServerName);

                if (connString == null)
                    return Json(new { error = $"Connection string '{ServerName}' not found." });

                var sql = LoadSqlFromDisk(ScriptName);

                using var conn = new SqlConnection(connString);
                using var cmd = new SqlCommand(sql, conn);

                var dt = new DataTable();
                conn.Open();
                dt.Load(cmd.ExecuteReader());

                var columns = dt.Columns.Cast<DataColumn>()
                    .Select(col => new { title = col.ColumnName, field = col.ColumnName })
                    .ToList();

                var rows = dt.AsEnumerable()
                    .Select(row => dt.Columns.Cast<DataColumn>().ToDictionary(
                        col => col.ColumnName,
                        col => row[col] == DBNull.Value ? null : row[col]
                    ))
                    .ToList();

                return Json(new { columns, rows });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }

        }
        [HttpPost]
        public JsonResult Index(string ScriptName, string ServerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ServerName))
                    return Json(new { error = "Select a server." });

                string connString = _configuration.GetConnectionString(ServerName);
                string sql = LoadSqlFromDisk(ScriptName);

                using var conn = new SqlConnection(connString);
                using var cmd = new SqlCommand(sql, conn);

                conn.Open();

                // STEP 1: Execute the SQL file (could be generator or normal query)
                using var reader = cmd.ExecuteReader();

                bool isGenerator = false;
                string generatedSql = null;

                // Detect generator: first resultset must be a single column called FinalSql
                if (reader.FieldCount == 1 &&
                    reader.GetName(0).Equals("FinalSql", StringComparison.OrdinalIgnoreCase))
                {
                    if (reader.Read())
                    {
                        generatedSql = reader.GetString(0);
                        isGenerator = true;
                    }
                }

                // -------------------------------
                // CASE A: It's a generator script
                // -------------------------------
                if (isGenerator)
                {
                    reader.Close();   // <-- ONLY CLOSE HERE

                    if (generatedSql.StartsWith("RAISEERROR"))
                        return Json(new { error = generatedSql });

                    using var cmd2 = new SqlCommand(generatedSql, conn);
                    using var reader2 = cmd2.ExecuteReader();

                    var dt = new DataTable();
                    dt.Load(reader2);

                    return Json(ToTabulator(dt));
                }

                // -------------------------------
                // CASE B: Normal SQL script
                // -------------------------------
                // DO NOT CLOSE reader here. Load it directly.
                var dtNormal = new DataTable();
                dtNormal.Load(reader);

                return Json(ToTabulator(dtNormal));
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.GetBaseException().Message });
            }
        }


        private object ToTabulator(DataTable dt)
        {
            var columns = dt.Columns.Cast<DataColumn>()
                .Select(col => new { title = col.ColumnName, field = col.ColumnName })
                .ToList();

            var rows = dt.Rows.Cast<DataRow>()
                .Select(row => dt.Columns.Cast<DataColumn>()
                    .ToDictionary(c => c.ColumnName, c => row[c] == DBNull.Value ? null : row[c])
                )
                .ToList();

            return new { columns, rows };
        }


        private string BuildHtmlTable(DataTable dt)
        {
            StringBuilder html = new StringBuilder();

            html.Append("<table class='table table-striped table-bordered'>");
            html.Append("<thead><tr>");
            foreach (DataColumn column in dt.Columns)
            {
                html.AppendFormat("<th>{0}</th>", column.ColumnName);
            }
            html.Append("</tr></thead><tbody>");

            foreach (DataRow row in dt.Rows)
            {
                html.Append("<tr>");
                foreach (var item in row.ItemArray)
                {
                    html.AppendFormat("<td>{0}</td>", item?.ToString());
                }
                html.Append("</tr>");
            }

            html.Append("</tbody></table>");
            return html.ToString();
        }


        [HttpPost]
        public JsonResult RunQuery([FromForm] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Json(new { error = "Please enter a SQL query." });


                // Normalize input
                string normalized = query.Trim().ToUpperInvariant();

                // Allow only SELECT or EXEC
                if (!(normalized.StartsWith("SELECT") || normalized.StartsWith("EXEC") || normalized.StartsWith("EXECUTE")))
                    return Json(new { error = "Only SELECT statements and stored procedure calls are allowed." });

                //// List of disallowed keywords (space included to reduce false positives)
                //string[] blocked = { "UPDATE ", "DELETE ", "DROP ", "ALTER ", "INSERT ", "TRUNCATE ", "CREATE ", "MERGE ", "GRANT ", "REVOKE ", "INTO " };

                //// Check for any blocked keyword
                //var found = blocked.FirstOrDefault(keyword => normalized.Contains(keyword));
                //if (found != null)
                //    return Json(new { error = $"Unsafe keyword detected: '{found.Trim()}' statements are not allowed." });


                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Convert DataTable to JSON-friendly format
                    var columns = new System.Collections.Generic.List<object>();
                    foreach (DataColumn col in dt.Columns)
                        columns.Add(new { title = col.ColumnName, field = col.ColumnName });

                    var rows = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        var dict = new System.Collections.Generic.Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                            dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                        rows.Add(dict);
                    }

                    return Json(new { columns, rows });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        private readonly IDashboardRepository _repository;

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentMetrics()
        {
            var salesForce = await _repository.GetSalesForceMetricsAsync();
            var crm = await _repository.GetCrmMetricsAsync();

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                salesForceProcessed = salesForce.Where(x => x.Processed).Sum(x => x.NumberOfRecords),
                salesForcePending = salesForce.Where(x => !x.Processed).Sum(x => x.NumberOfRecords),
                crmProcessed = crm.Where(x => x.Processed).Sum(x => x.NumberOfRecords),
                crmPending = crm.Where(x => !x.Processed).Sum(x => x.NumberOfRecords)
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] int hours = 4)
        {
            var since = DateTime.UtcNow.AddHours(-hours);
            var history = await _repository.GetMetricsHistoryAsync(since);
            return Ok(history);
        }
    }
}
