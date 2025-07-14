using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentRecentProposalsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/StudentRecentProposals/list
        [HttpGet("list")]
        public IActionResult GetRecentProposals()
        {
            var proposals = new List<object>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = @"
                    SELECT 
                        JSON_UNQUOTE(JSON_EXTRACT(Data, '$.projectTitle')) AS title,
                        Status,
                        SubmissionDate AS date,
                        JSON_UNQUOTE(JSON_EXTRACT(Data, '$.endDate')) AS deadline
                    FROM fyp_forms
                    ORDER BY SubmissionDate DESC
                    LIMIT 5";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string title = reader["title"]?.ToString() ?? "Untitled";
                    string status = reader["Status"]?.ToString() ?? "Unknown";
                    DateTime date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]) : DateTime.MinValue;

                    // Parse endDate (deadline) from JSON
                    DateTime deadline = DateTime.MinValue;
                    if (reader["deadline"] != DBNull.Value && DateTime.TryParse(reader["deadline"].ToString(), out var parsedDeadline))
                    {
                        deadline = parsedDeadline;
                    }

                    proposals.Add(new
                    {
                        title,
                        status,
                        date,
                        deadline,
                        progress = 100 // Placeholder; adjust if you implement actual progress logic
                    });
                }

                return Ok(proposals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error fetching recent FYP proposals.",
                    error = ex.Message
                });
            }
        }
    }
}
