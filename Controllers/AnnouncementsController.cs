using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementsController : ControllerBase
    {
        // private readonly string _connectionString = "Server=localhost;Database=ORIC;User=root;Password=;";
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        public AnnouncementsController()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var query = @"
                CREATE TABLE IF NOT EXISTS Announcements (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(255) NOT NULL,
                    Description TEXT NOT NULL,
                    PublishDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    Deadline DATETIME NULL,
                    TargetAudience VARCHAR(100) NOT NULL,
                    Status VARCHAR(50) NOT NULL DEFAULT 'Active'
                )";
            using var command = new MySqlCommand(query, connection);
            command.ExecuteNonQuery();
        }

        [HttpPost("add")]
        public IActionResult AddAnnouncement([FromBody] AnnouncementRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Received null request." });

            Console.WriteLine("✅ Received request: " + JsonSerializer.Serialize(request));

            if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Description) || string.IsNullOrEmpty(request.TargetAudience))
                return BadRequest(new { message = "Title, Description, and Target Audience are required." });

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var query = "INSERT INTO Announcements (Title, Description, PublishDate, Deadline, TargetAudience, Status) VALUES (@Title, @Description, @PublishDate, @Deadline, @TargetAudience, @Status)";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@PublishDate", request.PublishDate ?? DateTime.UtcNow);
            command.Parameters.AddWithValue("@Deadline", request.Deadline ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TargetAudience", request.TargetAudience);
            command.Parameters.AddWithValue("@Status", request.Status ?? "Active");
            command.ExecuteNonQuery();

            return Ok(new { message = "Announcement added successfully!" });
        }

        [HttpGet("all")]
        public IActionResult GetAllAnnouncements()
        {
            List<AnnouncementResponse> announcements = new();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var query = "SELECT Id, Title, Description, PublishDate, Deadline, TargetAudience, Status FROM Announcements";
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                announcements.Add(new AnnouncementResponse
                {
                    Id = reader.GetInt32("Id"),
                    Title = reader.GetString("Title"),
                    Description = reader.GetString("Description"),
                    PublishDate = reader.GetDateTime("PublishDate"),
                    Deadline = reader.IsDBNull("Deadline") ? (DateTime?)null : reader.GetDateTime("Deadline"),
                    TargetAudience = reader.GetString("TargetAudience"),
                    Status = reader.GetString("Status")
                });
            }

            return Ok(announcements);
        }

        [HttpPut("update/{id}")]
        public IActionResult UpdateAnnouncement(int id, [FromBody] AnnouncementRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Received null request." });

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var query = "UPDATE Announcements SET Title = @Title, Description = @Description, Deadline = @Deadline, TargetAudience = @TargetAudience, Status = @Status WHERE Id = @Id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.Parameters.AddWithValue("@Deadline", request.Deadline ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TargetAudience", request.TargetAudience);
            command.Parameters.AddWithValue("@Status", request.Status ?? "Active");
            command.Parameters.AddWithValue("@Id", id);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
                return NotFound(new { message = "Announcement not found." });

            return Ok(new { message = "Announcement updated successfully!" });
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteAnnouncement(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            var query = "DELETE FROM Announcements WHERE Id = @Id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
                return NotFound(new { message = "Announcement not found." });

            return Ok(new { message = "Announcement deleted successfully!" });
        }
    }

    public class AnnouncementRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("publishDate")]
        public DateTime? PublishDate { get; set; }

        [JsonPropertyName("deadline")]
        public DateTime? Deadline { get; set; }

        [JsonPropertyName("targetAudience")]
        public string TargetAudience { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";
    }

    public class AnnouncementResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? Deadline { get; set; }
        public string TargetAudience { get; set; }
        public string Status { get; set; }
    }
}
