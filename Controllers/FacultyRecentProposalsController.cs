using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyRecentProposalsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/FacultyRecentProposals/list
        [HttpGet("list")]
        public IActionResult GetRecentProposals()
        {
            var proposals = new List<ProposalItem>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT ProposalTitle, SubmissionDate, Status 
                    FROM faculty_proposals 
                    ORDER BY SubmissionDate DESC 
                    LIMIT 5";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    proposals.Add(new ProposalItem
                    {
                        Title = reader.GetString("ProposalTitle"),
                        SubmissionDate = reader.GetDateTime("SubmissionDate"),
                        Status = reader.GetString("Status")
                    });
                }

                return Ok(proposals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching recent proposals.", error = ex.Message });
            }
        }

        private class ProposalItem
        {
            public string Title { get; set; }
            public DateTime SubmissionDate { get; set; }
            public string Status { get; set; }
        }
    }
}
