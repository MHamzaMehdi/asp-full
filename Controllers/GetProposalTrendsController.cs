using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetProposalTrendsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/GetProposalTrends/monthly
        [HttpGet("monthly")]
        public IActionResult GetMonthlyProposalTrends()
        {
            var monthCounts = new Dictionary<string, int>(); // key = "MMM yyyy"

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // --- Fetch research proposals by month ---
                string researchQuery = @"
                    SELECT DATE_FORMAT(SubmissionDate, '%Y-%m') AS Month, COUNT(*) AS Count
                    FROM faculty_proposals
                    WHERE SubmissionDate >= DATE_SUB(CURDATE(), INTERVAL 6 MONTH)
                    GROUP BY Month";

                using var researchCmd = new MySqlCommand(researchQuery, connection);
                using var researchReader = researchCmd.ExecuteReader();
                while (researchReader.Read())
                {
                    var monthKey = readerMonthKey(researchReader.GetString("Month"));
                    int count = researchReader.GetInt32("Count");

                    if (monthCounts.ContainsKey(monthKey))
                        monthCounts[monthKey] += count;
                    else
                        monthCounts[monthKey] = count;
                }
                researchReader.Close();

                // --- Fetch FYP proposals by month ---
                string fypQuery = @"
                    SELECT DATE_FORMAT(CreatedAt, '%Y-%m') AS Month, COUNT(*) AS Count
                    FROM fyp_forms
                    WHERE CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 6 MONTH)
                    GROUP BY Month";

                using var fypCmd = new MySqlCommand(fypQuery, connection);
                using var fypReader = fypCmd.ExecuteReader();
                while (fypReader.Read())
                {
                    var monthKey = readerMonthKey(fypReader.GetString("Month"));
                    int count = fypReader.GetInt32("Count");

                    if (monthCounts.ContainsKey(monthKey))
                        monthCounts[monthKey] += count;
                    else
                        monthCounts[monthKey] = count;
                }

                // --- Ensure exactly 6 months are returned (even if 0 submissions) ---
                var now = DateTime.Now;
                var finalList = Enumerable.Range(0, 6)
                    .Select(i => now.AddMonths(-i))
                    .OrderBy(d => d)
                    .Select(date =>
                    {
                        var key = date.ToString("MMM yyyy");
                        return new
                        {
                            month = key,
                            count = monthCounts.ContainsKey(key) ? monthCounts[key] : 0
                        };
                    })
                    .ToList();

                return Ok(finalList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching monthly trends.", error = ex.Message });
            }
        }

        // Helper to format 'yyyy-MM' into 'MMM yyyy'
        private string readerMonthKey(string dbMonth)
        {
            if (DateTime.TryParseExact(dbMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToString("MMM yyyy");
            }
            return dbMonth;
        }
    }
}
