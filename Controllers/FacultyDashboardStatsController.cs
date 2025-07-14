using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyDashboardStatsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/FacultyDashboardStats/my-proposals/count
        [HttpGet("my-proposals/count")]
        public IActionResult GetMyProposalsCount()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = "SELECT COUNT(*) FROM faculty_proposals";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalMyProposals = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching proposals count.", error = ex.Message });
            }
        }

        // GET: /api/FacultyDashboardStats/active-proposals/count
        [HttpGet("active-proposals/count")]
        public IActionResult GetActiveProposalsCount()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = "SELECT COUNT(*) FROM callforproposals WHERE Status = 'Active'";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalActiveProposals = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching active proposals count.", error = ex.Message });
            }
        }

        // GET: /api/FacultyDashboardStats/announcements/count
        [HttpGet("announcements/count")]
        public IActionResult GetFacultyAnnouncementsCount()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM announcements 
                    WHERE TargetAudience IN ('Faculty', 'All')";

                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalAnnouncements = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching announcements count.", error = ex.Message });
            }
        }

        // GET: /api/FacultyDashboardStats/upcoming/count
        [HttpGet("upcoming/count")]
        public IActionResult GetUpcomingEventsCount()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT COUNT(*) 
                    FROM calendarevents 
                    WHERE EventDate >= CURDATE()
                      AND Audience IN ('faculty', 'all')";

                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalUpcomingEvents = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching upcoming events count.", error = ex.Message });
            }
        }
    }
}
