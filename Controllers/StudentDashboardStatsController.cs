using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentDashboardStatsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/StudentDashboardStats/my-proposals/count
        [HttpGet("my-proposals/count")]
        public IActionResult GetMyProposals()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM fyp_forms";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalMyProposals = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching student proposals count.", error = ex.Message });
            }
        }

        // GET: /api/StudentDashboardStats/available-proposals/count
        [HttpGet("available-proposals/count")]
        public IActionResult GetAvailableProposals()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM fyp_proposals WHERE status = 'open' AND deadline >= CURDATE()";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalAvailableProposals = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching available proposals count.", error = ex.Message });
            }
        }

        // GET: /api/StudentDashboardStats/announcements/count
        [HttpGet("announcements/count")]
        public IActionResult GetStudentAnnouncements()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM announcements WHERE TargetAudience IN ('student', 'all')";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalStudentAnnouncements = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching student announcements count.", error = ex.Message });
            }
        }

        // GET: /api/StudentDashboardStats/upcoming/count
        [HttpGet("upcoming/count")]
        public IActionResult GetUpcomingEvents()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM calendarevents WHERE Audience IN ('student', 'all') AND EventDate >= CURDATE()";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalUpcoming = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching upcoming events count.", error = ex.Message });
            }
        }
    }
}
