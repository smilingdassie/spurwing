using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PosItemVerificationWeb.Models;
using System.Data;
using System.Text;
using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PosItemVerificationWeb.Helpers;

namespace PosItemVerificationWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        private readonly AllowedUsersConfig _allowed;

        public HomeController(IConfiguration configuration, IOptions<AllowedUsersConfig> allowed)
        {
            _connectionString = configuration.GetConnectionString("RestaurantOpeningConnection");
            _allowed = allowed.Value;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var user = User.NormalizedName();

            if (!_allowed.DashboardList.Contains(user))
            {
                return Unauthorized();
            }
            return RedirectToAction("Index", "POSItemVerification");
            //return View(new SqlQueryResult());
        }

        [HttpPost]
        public ActionResult Index(SqlQueryResult model)
        {
            if (string.IsNullOrWhiteSpace(model.Query))
            {
                model.ErrorMessage = "Please enter a SQL query.";
                return View(model);
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(model.Query, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    model.HtmlTable = BuildHtmlTable(dt);
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }

            return View(model);
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

                // List of disallowed keywords (space included to reduce false positives)
                string[] blocked = { "UPDATE ", "DELETE ", "DROP ", "ALTER ", "INSERT ", "TRUNCATE ", "CREATE ", "MERGE ", "GRANT ", "REVOKE ", "INTO " };

                // Check for any blocked keyword
                var found = blocked.FirstOrDefault(keyword => normalized.Contains(keyword));
                if (found != null)
                    return Json(new { error = $"Unsafe keyword detected: '{found.Trim()}' statements are not allowed." });


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
    }


}



