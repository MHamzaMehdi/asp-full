using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetRecentActivitiesController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/GetRecentActivities/list
        [HttpGet("list")]
        public IActionResult GetRecentActivities()
        {
            var activities = new List<ActivityItem>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // --- Recent Research Proposals ---
                string researchQuery = @"
                    SELECT ProposalTitle, SubmissionDate, Status 
                    FROM faculty_proposals 
                    ORDER BY SubmissionDate DESC 
                    LIMIT 5";

                using var researchCmd = new MySqlCommand(researchQuery, connection);
                using var researchReader = researchCmd.ExecuteReader();
                while (researchReader.Read())
                {
                    activities.Add(new ActivityItem
                    {
                        Action = "New Research Proposal",
                        Subject = researchReader.GetString("ProposalTitle"),
                        Time = GetTimeAgo(researchReader.GetDateTime("SubmissionDate")),
                        Status = researchReader.GetString("Status")
                    });
                }
                researchReader.Close();

                // --- Recent FYP Proposals ---
                string fypQuery = @"
                    SELECT UserName, CreatedAt, Status 
                    FROM fyp_forms 
                    ORDER BY CreatedAt DESC 
                    LIMIT 5";

                using var fypCmd = new MySqlCommand(fypQuery, connection);
                using var fypReader = fypCmd.ExecuteReader();
                while (fypReader.Read())
                {
                    activities.Add(new ActivityItem
                    {
                        Action = "New FYP Submission",
                        Subject = fypReader.GetString("UserName"),
                        Time = GetTimeAgo(fypReader.GetDateTime("CreatedAt")),
                        Status = fypReader.GetString("Status")
                    });
                }
                fypReader.Close();

                // --- Recent Faculty Users ---
                string userQuery = @"
                    SELECT name, last_active, isActive 
                    FROM user 
                    WHERE role = 'faculty'
                    ORDER BY last_active DESC 
                    LIMIT 5";

                using var userCmd = new MySqlCommand(userQuery, connection);
                using var userReader = userCmd.ExecuteReader();
                while (userReader.Read())
                {
                    activities.Add(new ActivityItem
                    {
                        Action = "New Faculty User",
                        Subject = userReader.GetString("name"),
                        Time = GetTimeAgo(userReader.GetDateTime("last_active")),
                        Status = userReader.GetBoolean("isActive") ? "Active" : "Inactive"
                    });
                }

                // Sort all combined items by most recent time
                var sorted = activities
                    .OrderByDescending(a => a.RawTime)
                    .Take(6)
                    .Select(a => new
                    {
                        a.Action,
                        a.Subject,
                        a.Time,
                        a.Status
                    })
                    .ToList();

                return Ok(sorted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching recent activities.", error = ex.Message });
            }
        }

        // Helper DTO
        private class ActivityItem
        {
            public string Action { get; set; }
            public string Subject { get; set; }
            public string Time { get; set; } // human-readable
            public string Status { get; set; }

            // For sorting
            public DateTime RawTime { get; set; }
        }

        // Helper to get "2 hours ago", etc.
        private string GetTimeAgo(DateTime dateTime)
        {
            TimeSpan timeDiff = DateTime.Now - dateTime;

            if (timeDiff.TotalMinutes < 60)
                return $"{(int)timeDiff.TotalMinutes} minutes ago";
            else if (timeDiff.TotalHours < 24)
                return $"{(int)timeDiff.TotalHours} hours ago";
            else if (timeDiff.TotalDays < 2)
                return "Yesterday";
            else
                return $"{(int)timeDiff.TotalDays} days ago";
        }
    }
}
