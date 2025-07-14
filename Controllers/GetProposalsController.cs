using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetProposalsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/GetProposals/latest
        [HttpGet("latest")]
        public IActionResult GetLatestProposals()
        {
            var combinedProposals = new List<ProposalSummary>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // ----- Research Proposals -----
                string researchQuery = @"
                    SELECT ProposalTitle AS Title, FacultyName AS SubmittedBy, SubmissionDate AS Date, Status 
                    FROM faculty_proposals 
                    ORDER BY SubmissionDate DESC 
                    LIMIT 5";

                using var researchCmd = new MySqlCommand(researchQuery, connection);
                using var researchReader = researchCmd.ExecuteReader();
                while (researchReader.Read())
                {
                    combinedProposals.Add(new ProposalSummary
                    {
                        Title = researchReader.GetString("Title"),
                        Type = "Research",
                        SubmittedBy = researchReader.GetString("SubmittedBy"),
                        Date = researchReader.GetDateTime("Date"),
                        Status = researchReader.GetString("Status")
                    });
                }
                researchReader.Close();

                // ----- FYP Proposals -----
                string fypQuery = @"
                    SELECT UserName AS SubmittedBy, CreatedAt AS Date, Status 
                    FROM fyp_forms 
                    ORDER BY CreatedAt DESC 
                    LIMIT 5";

                using var fypCmd = new MySqlCommand(fypQuery, connection);
                using var fypReader = fypCmd.ExecuteReader();
                while (fypReader.Read())
                {
                    combinedProposals.Add(new ProposalSummary
                    {
                        Title = "FYP Proposal",
                        Type = "FYP",
                        SubmittedBy = fypReader.GetString("SubmittedBy"),
                        Date = fypReader.GetDateTime("Date"),
                        Status = fypReader.GetString("Status")
                    });
                }

                // Sort both combined proposals by latest date and limit to top 5
                var latestCombined = combinedProposals
                    .OrderByDescending(p => p.Date)
                    .Take(5)
                    .ToList();

                return Ok(latestCombined);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching latest proposals.", error = ex.Message });
            }
        }

        // DTO for simplified dashboard view
        private class ProposalSummary
        {
            public string Title { get; set; }
            public string Type { get; set; }
            public string SubmittedBy { get; set; }
            public DateTime Date { get; set; }
            public string Status { get; set; }
        }
    }
}
