using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallForProposalController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        public CallForProposalController()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    CREATE TABLE IF NOT EXISTS CallForProposals (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(255) NOT NULL,
                        FundingAgency VARCHAR(255) NOT NULL,
                        AreaOfInterest VARCHAR(255) NOT NULL,
                        Description TEXT NOT NULL,
                        Deadline DATETIME NOT NULL,
                        Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                        SubmissionLink TEXT NULL
                    )";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // ✅ API to Add a New Call for Proposal
        [HttpPost("add")]
        public IActionResult AddProposal([FromBody] CallForProposalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.FundingAgency) ||
                string.IsNullOrEmpty(request.AreaOfInterest) || string.IsNullOrEmpty(request.Description) || request.Deadline == default)
            {
                return BadRequest("Invalid request data.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    INSERT INTO CallForProposals (Title, FundingAgency, AreaOfInterest, Description, Deadline, Status, SubmissionLink) 
                    VALUES (@Title, @FundingAgency, @AreaOfInterest, @Description, @Deadline, @Status, @SubmissionLink)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", request.Title);
                    command.Parameters.AddWithValue("@FundingAgency", request.FundingAgency);
                    command.Parameters.AddWithValue("@AreaOfInterest", request.AreaOfInterest);
                    command.Parameters.AddWithValue("@Description", request.Description);
                    command.Parameters.AddWithValue("@Deadline", request.Deadline);
                    command.Parameters.AddWithValue("@Status", request.Status ?? "Pending");
                    command.Parameters.AddWithValue("@SubmissionLink", request.SubmissionLink);
                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Call for proposal added successfully!" });
        }

        // ✅ API to Get All Proposals
        [HttpGet("all")]
        public IActionResult GetAllProposals()
        {
            List<CallForProposalResponse> proposals = new List<CallForProposalResponse>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Title, FundingAgency, AreaOfInterest, Description, Deadline, Status, SubmissionLink FROM CallForProposals";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        proposals.Add(new CallForProposalResponse
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            FundingAgency = reader.GetString("FundingAgency"),
                            AreaOfInterest = reader.GetString("AreaOfInterest"),
                            Description = reader.GetString("Description"),
                            Deadline = reader.GetDateTime("Deadline"),
                            Status = reader.GetString("Status"),
                            SubmissionLink = reader.IsDBNull(reader.GetOrdinal("SubmissionLink")) ? null : reader.GetString("SubmissionLink")
                        });
                    }
                }
            }

            return Ok(proposals);
        }

        // ✅ API to Update an Existing Proposal
        [HttpPut("update/{id}")]
        public IActionResult UpdateProposal(int id, [FromBody] CallForProposalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.FundingAgency) ||
                string.IsNullOrEmpty(request.AreaOfInterest) || string.IsNullOrEmpty(request.Description) || request.Deadline == default)
            {
                return BadRequest("Invalid request data.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Check if the proposal exists
                var checkQuery = "SELECT COUNT(*) FROM CallForProposals WHERE Id = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count == 0)
                    {
                        return NotFound("Proposal not found.");
                    }
                }

                // Update the proposal
                var updateQuery = @"
                    UPDATE CallForProposals 
                    SET Title = @Title, FundingAgency = @FundingAgency, AreaOfInterest = @AreaOfInterest, 
                        Description = @Description, Deadline = @Deadline, Status = @Status, SubmissionLink = @SubmissionLink 
                    WHERE Id = @Id";

                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@Id", id);
                    updateCommand.Parameters.AddWithValue("@Title", request.Title);
                    updateCommand.Parameters.AddWithValue("@FundingAgency", request.FundingAgency);
                    updateCommand.Parameters.AddWithValue("@AreaOfInterest", request.AreaOfInterest);
                    updateCommand.Parameters.AddWithValue("@Description", request.Description);
                    updateCommand.Parameters.AddWithValue("@Deadline", request.Deadline);
                    updateCommand.Parameters.AddWithValue("@Status", request.Status);
                    updateCommand.Parameters.AddWithValue("@SubmissionLink", request.SubmissionLink);
                    updateCommand.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Call for proposal updated successfully!" });
        }

        // ✅ API to Delete a Proposal
        [HttpDelete("delete/{id}")]
        public IActionResult DeleteProposal(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                // Check if the proposal exists
                var checkQuery = "SELECT COUNT(*) FROM CallForProposals WHERE Id = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    var count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count == 0)
                    {
                        return NotFound("Proposal not found.");
                    }
                }

                // Delete the proposal
                var deleteQuery = "DELETE FROM CallForProposals WHERE Id = @Id";
                using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@Id", id);
                    deleteCommand.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Call for proposal deleted successfully!" });
        }
    }

    // ✅ Request Model for Adding/Updating Proposal
    public class CallForProposalRequest
    {
        public string Title { get; set; }
        public string FundingAgency { get; set; }
        public string AreaOfInterest { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "Pending";
        public string SubmissionLink { get; set; }
    }

    // ✅ Response Model for Retrieving Proposals
    public class CallForProposalResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FundingAgency { get; set; }
        public string AreaOfInterest { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; }
        public string SubmissionLink { get; set; }
    }
}
