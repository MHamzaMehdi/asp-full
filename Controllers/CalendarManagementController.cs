using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarManagementController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        public CalendarManagementController()
        {
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = @"
                CREATE TABLE IF NOT EXISTS CalendarEvents (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Title VARCHAR(255) NOT NULL,
                    EventDate DATETIME NOT NULL,
                    Time TIME,
                    Description TEXT,
                    Type ENUM('meeting', 'review', 'event', 'deadline') NOT NULL,
                    Audience ENUM('admin', 'faculty', 'student', 'all') NOT NULL
                );";

            using var command = new MySqlCommand(query, connection);
            command.ExecuteNonQuery();
        }

        [HttpPost("oric/add")]
        public IActionResult AddOricEvent([FromBody] OricEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Date))
                return BadRequest("Missing title or date.");

            if (!DateTime.TryParse(request.Date, out DateTime eventDate))
                return BadRequest("Invalid date format.");

            TimeSpan? parsedTime = null;
            if (!string.IsNullOrWhiteSpace(request.Time))
            {
                if (!TimeSpan.TryParse(request.Time, out TimeSpan ts))
                    return BadRequest("Invalid time format.");
                parsedTime = ts;
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = @"
                INSERT INTO CalendarEvents (Title, EventDate, Time, Description, Type, Audience)
                VALUES (@Title, @EventDate, @Time, @Description, @Type, 'admin');";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@EventDate", eventDate);
            command.Parameters.AddWithValue("@Time", parsedTime.HasValue ? parsedTime.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", request.Type);

            command.ExecuteNonQuery();
            return Ok(new { message = "ORIC event added successfully." });
        }

        [HttpGet("oric/all")]
        public IActionResult GetAllOricEvents() => Ok(GetEventsByAudience("admin"));

        [HttpPut("oric/update/{id}")]
        public IActionResult UpdateOricEvent(int id, [FromBody] OricEventRequest request) => UpdateOricEventInternal(id, request);

        private IActionResult UpdateOricEventInternal(int id, OricEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Date))
                return BadRequest("Missing title or date.");

            if (!DateTime.TryParse(request.Date, out DateTime eventDate))
                return BadRequest("Invalid date format.");

            TimeSpan? parsedTime = null;
            if (!string.IsNullOrWhiteSpace(request.Time))
            {
                if (!TimeSpan.TryParse(request.Time, out TimeSpan ts))
                    return BadRequest("Invalid time format.");
                parsedTime = ts;
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var existsQuery = "SELECT COUNT(*) FROM CalendarEvents WHERE Id = @Id AND Audience = 'admin'";
            using var check = new MySqlCommand(existsQuery, connection);
            check.Parameters.AddWithValue("@Id", id);

            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                return NotFound("Event not found.");

            var updateQuery = @"
                UPDATE CalendarEvents 
                SET Title = @Title, EventDate = @EventDate, Time = @Time,
                    Description = @Description, Type = @Type
                WHERE Id = @Id;";

            using var cmd = new MySqlCommand(updateQuery, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Title", request.Title);
            cmd.Parameters.AddWithValue("@EventDate", eventDate);
            cmd.Parameters.AddWithValue("@Time", parsedTime.HasValue ? parsedTime.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", request.Type);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Event updated successfully." });
        }

        [HttpDelete("oric/delete/{id}")]
        public IActionResult DeleteOricEvent(int id) => DeleteEvent(id, "admin");

        [HttpPost("target/add")]
        public IActionResult AddTargetEvent([FromBody] TargetEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Date))
                return BadRequest("Missing title or date.");

            if (!DateTime.TryParse(request.Date, out DateTime eventDate))
                return BadRequest("Invalid date format.");

            TimeSpan? parsedTime = null;
            if (!string.IsNullOrWhiteSpace(request.Time))
            {
                if (!TimeSpan.TryParse(request.Time, out TimeSpan ts))
                    return BadRequest("Invalid time format.");
                parsedTime = ts;
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = @"
                INSERT INTO CalendarEvents (Title, EventDate, Time, Description, Type, Audience)
                VALUES (@Title, @EventDate, @Time, @Description, @Type, @Audience);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@EventDate", eventDate);
            command.Parameters.AddWithValue("@Time", parsedTime.HasValue ? parsedTime.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Type", request.Type);
            command.Parameters.AddWithValue("@Audience", request.Audience);

            command.ExecuteNonQuery();
            return Ok(new { message = "Target audience event added successfully." });
        }

        [HttpGet("target/all")]
        public IActionResult GetAllTargetEvents() => Ok(GetEventsByAudience("faculty", "student", "all"));

        [HttpPut("target/update/{id}")]
        public IActionResult UpdateTargetEvent(int id, [FromBody] TargetEventRequest request) => UpdateEvent(id, request, request.Audience);

        [HttpDelete("target/delete/{id}")]
        public IActionResult DeleteTargetEvent(int id) => DeleteEvent(id, null);

        private List<CalendarEventResponse> GetEventsByAudience(params string[] audiences)
        {
            var results = new List<CalendarEventResponse>();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            var query = $"SELECT * FROM CalendarEvents WHERE Audience IN ('{string.Join("','", audiences)}')";
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new CalendarEventResponse
                {
                    Id = reader.GetInt32("Id"),
                    Title = reader.GetString("Title"),
                    EventDate = reader.GetDateTime("EventDate"),
                    Time = reader.IsDBNull(reader.GetOrdinal("Time")) ? null : reader.GetTimeSpan("Time"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                    Type = reader.GetString("Type"),
                    Audience = reader.GetString("Audience")
                });
            }

            return results;
        }

        private IActionResult UpdateEvent(int id, TargetEventRequest request, string expectedAudience)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Date))
                return BadRequest("Missing title or date.");

            if (!DateTime.TryParse(request.Date, out DateTime eventDate))
                return BadRequest("Invalid date format.");

            TimeSpan? parsedTime = null;
            if (!string.IsNullOrWhiteSpace(request.Time))
            {
                if (!TimeSpan.TryParse(request.Time, out TimeSpan ts))
                    return BadRequest("Invalid time format.");
                parsedTime = ts;
            }

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string audienceCondition = expectedAudience != null ? "AND Audience = @Audience" : "";
            var existsQuery = $"SELECT COUNT(*) FROM CalendarEvents WHERE Id = @Id {audienceCondition}";

            using var check = new MySqlCommand(existsQuery, connection);
            check.Parameters.AddWithValue("@Id", id);
            if (expectedAudience != null)
                check.Parameters.AddWithValue("@Audience", expectedAudience);

            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                return NotFound("Event not found.");

            var updateQuery = @"
                UPDATE CalendarEvents 
                SET Title = @Title, EventDate = @EventDate, Time = @Time,
                    Description = @Description, Type = @Type, Audience = @Audience
                WHERE Id = @Id;";

            using var cmd = new MySqlCommand(updateQuery, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Title", request.Title);
            cmd.Parameters.AddWithValue("@EventDate", eventDate);
            cmd.Parameters.AddWithValue("@Time", parsedTime.HasValue ? parsedTime.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Type", request.Type);
            cmd.Parameters.AddWithValue("@Audience", request.Audience);

            cmd.ExecuteNonQuery();
            return Ok(new { message = "Event updated successfully." });
        }

        private IActionResult DeleteEvent(int id, string? audience)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string condition = audience != null ? "AND Audience = @Audience" : "";
            var checkQuery = $"SELECT COUNT(*) FROM CalendarEvents WHERE Id = @Id {condition}";

            using var check = new MySqlCommand(checkQuery, connection);
            check.Parameters.AddWithValue("@Id", id);
            if (audience != null)
                check.Parameters.AddWithValue("@Audience", audience);

            if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                return NotFound("Event not found.");

            var deleteQuery = "DELETE FROM CalendarEvents WHERE Id = @Id";
            using var cmd = new MySqlCommand(deleteQuery, connection);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Event deleted successfully." });
        }
    }

    public class OricEventRequest
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; }
    }

    public class TargetEventRequest
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; }
        public string Audience { get; set; }
    }

    public class CalendarEventResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime EventDate { get; set; }
        public TimeSpan? Time { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; }
        public string Audience { get; set; }
    }
}
