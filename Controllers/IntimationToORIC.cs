using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntimationToORIC : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";
        private const int MaxFileSizeInBytes = 10 * 1024 * 1024; // 10MB
        private readonly string[] AllowedFileTypes = new[] { ".pdf", ".doc", ".docx" };
        private static readonly Dictionary<string, string> _mimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
        };

        public IntimationToORIC()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = @"
                CREATE TABLE IF NOT EXISTS faculty_proposals (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    ProposalTitle VARCHAR(255) NOT NULL,
                    FacultyName VARCHAR(255) NOT NULL,
                    Department VARCHAR(255) NOT NULL,
                    SubmissionDate DATE NOT NULL,
                    Description TEXT,
                    SupportingDocuments LONGBLOB NULL,
                    FileType VARCHAR(10) NULL,
                    Status VARCHAR(50) NOT NULL DEFAULT 'Under Review'  -- Added status field
                )";
            using var command = new MySqlCommand(query, connection);
            command.ExecuteNonQuery();
        }

        [HttpGet("get-faculty-proposals")]
        public IActionResult GetFacultyProposals()
        {
            var proposals = new List<FacultyProposalResponse>();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT Id, ProposalTitle, FacultyName, Department, SubmissionDate, FileType, Status, Description FROM faculty_proposals";
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                proposals.Add(new FacultyProposalResponse
                {
                    Id = reader.GetInt32("Id"),
                    ProposalTitle = reader.GetString("ProposalTitle"),
                    FacultyName = reader.GetString("FacultyName"),
                    Department = reader.GetString("Department"),
                    SubmissionDate = reader.GetDateTime("SubmissionDate"),
                    FileType = reader.IsDBNull("FileType") ? "" : reader.GetString("FileType"),
                    Status = reader.IsDBNull("Status") ? "Under Review" : reader.GetString("Status"),
                    Description = reader.IsDBNull("Description") ? "" : reader.GetString("Description")
                });
            }

            return Ok(proposals);
        }


        [HttpPost("add-faculty-proposal")]
        public IActionResult AddFacultyProposal([FromForm] FacultyProposalRequest request, IFormFile file)
        {
            // Log the incoming fields
            Console.WriteLine($"Received ProposalTitle: {request.ProposalTitle}");
            Console.WriteLine($"Received FacultyName: {request.FacultyName}");
            Console.WriteLine($"Received Department: {request.Department}");
            Console.WriteLine($"Received SubmissionDate: {request.SubmissionDate}");
            Console.WriteLine($"Received Description: {request.Description ?? "No description"}");

            if (file != null)
            {
                Console.WriteLine($"Received File: {file.FileName} with size {file.Length} bytes");
            }
            else
            {
                Console.WriteLine("No file uploaded");
            }

            if (request == null || string.IsNullOrEmpty(request.ProposalTitle) ||
                string.IsNullOrEmpty(request.FacultyName) || string.IsNullOrEmpty(request.Department) ||
                request.SubmissionDate == default)
            {
                return BadRequest(new { message = "Missing required proposal information." });
            }

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Supporting document is required." });

            if (file.Length > MaxFileSizeInBytes)
                return BadRequest(new { message = "File size exceeds 10MB." });

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedFileTypes.Contains(fileExtension))
                return BadRequest(new { message = "Invalid file type. Allowed: PDF, DOC, DOCX" });

            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileData = memoryStream.ToArray();
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string insertQuery = @"
        INSERT INTO faculty_proposals (
            ProposalTitle, FacultyName, Department, SubmissionDate, Description,
            SupportingDocuments, FileType, Status
        ) VALUES (
            @ProposalTitle, @FacultyName, @Department, @SubmissionDate, @Description,
            @SupportingDocuments, @FileType, 'Under Review'
        )";

            using var command = new MySqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@ProposalTitle", request.ProposalTitle);
            command.Parameters.AddWithValue("@FacultyName", request.FacultyName);
            command.Parameters.AddWithValue("@Department", request.Department);
            command.Parameters.AddWithValue("@SubmissionDate", request.SubmissionDate);
            command.Parameters.AddWithValue("@Description", request.Description ?? "");
            command.Parameters.Add("@SupportingDocuments", MySqlDbType.LongBlob).Value = fileData;
            command.Parameters.AddWithValue("@FileType", fileExtension);

            int rows = command.ExecuteNonQuery();
            if (rows > 0)
            {
                return Ok(new { message = "Proposal added successfully!" });
            }

            return StatusCode(500, new { message = "Failed to save proposal." });
        }



        [HttpPut("update-faculty-proposal/{id}")]
        public IActionResult UpdateFacultyProposal(int id, [FromBody] FacultyProposalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ProposalTitle) ||
                string.IsNullOrEmpty(request.FacultyName) || string.IsNullOrEmpty(request.Department) ||
                request.SubmissionDate == default)
            {
                return BadRequest("Invalid request data.");
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var checkQuery = "SELECT COUNT(*) FROM faculty_proposals WHERE Id = @Id";
            using var checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Id", id);
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                return NotFound("Proposal not found.");

            var updateQuery = @"
        UPDATE faculty_proposals 
        SET ProposalTitle = @ProposalTitle,
            FacultyName = @FacultyName,
            Department = @Department,
            SubmissionDate = @SubmissionDate,
            Description = @Description,
            Status = @Status
        WHERE Id = @Id";  // Update status as passed in the request

            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@Id", id);
            updateCmd.Parameters.AddWithValue("@ProposalTitle", request.ProposalTitle);
            updateCmd.Parameters.AddWithValue("@FacultyName", request.FacultyName);
            updateCmd.Parameters.AddWithValue("@Department", request.Department);
            updateCmd.Parameters.AddWithValue("@SubmissionDate", request.SubmissionDate);
            updateCmd.Parameters.AddWithValue("@Description", request.Description ?? "");
            updateCmd.Parameters.AddWithValue("@Status", request.Status);  // Set the status to the new value

            updateCmd.ExecuteNonQuery();
            return Ok(new { message = "Proposal updated successfully!" });
        }


        [HttpDelete("delete-faculty-proposal/{id}")]
        public IActionResult DeleteFacultyProposal(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string checkQuery = "SELECT COUNT(*) FROM faculty_proposals WHERE Id = @Id";
            using var checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Id", id);
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                return NotFound("Proposal not found.");

            string deleteQuery = "DELETE FROM faculty_proposals WHERE Id = @Id";
            using var deleteCmd = new MySqlCommand(deleteQuery, connection);
            deleteCmd.Parameters.AddWithValue("@Id", id);
            deleteCmd.ExecuteNonQuery();

            return Ok(new { message = "Proposal deleted successfully!" });
        }

        [HttpGet("download-faculty-proposal-file/{id}")]
        public IActionResult DownloadFacultyProposalFile(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = "SELECT ProposalTitle, SupportingDocuments, FileType FROM faculty_proposals WHERE Id = @Id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                if (reader.IsDBNull("SupportingDocuments"))
                    return NotFound("No file available.");

                var fileData = (byte[])reader["SupportingDocuments"];
                var proposalTitle = reader.GetString("ProposalTitle");
                var fileType = reader.GetString("FileType").ToLower();

                if (!_mimeTypes.TryGetValue(fileType, out var contentType))
                    contentType = "application/octet-stream";

                var safeFileName = string.Join("_", proposalTitle.Split(Path.GetInvalidFileNameChars())) + fileType;

                Response.Headers.Clear();
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{safeFileName}\"");
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                Response.Headers.Add("Cache-Control", "no-cache, must-revalidate, max-age=0");
                Response.Headers.Add("Pragma", "no-cache");

                return File(fileData, contentType);
            }

            return NotFound("Proposal not found.");
        }
    }

    public class FacultyProposalRequest
    {
        public string ProposalTitle { get; set; }
        public string FacultyName { get; set; }
        public string Department { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Description { get; set; }
        public string? Status { get; set; }  // Added Status field
    }

    public class FacultyProposalResponse
    {
        public int Id { get; set; }
        public string ProposalTitle { get; set; }
        public string FacultyName { get; set; }
        public string Department { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string FileType { get; set; }
        public string Status { get; set; }  // Added Status field
        public string Description { get; set; }
    }
}
