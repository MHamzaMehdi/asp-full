using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetDashboardStatsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/GetDashboardStats/proposals/count
        [HttpGet("proposals/count")]
        public IActionResult GetTotalProposals()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM faculty_proposals";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalProposals = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching proposal count.", error = ex.Message });
            }
        }

        // GET: /api/GetDashboardStats/faculty-users/count
        [HttpGet("faculty-users/count")]
        public IActionResult GetTotalFacultyUsers()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM user WHERE role = 'faculty'";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalFacultyUsers = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching faculty user count.", error = ex.Message });
            }
        }

        // GET: /api/GetDashboardStats/student-projects/count
        [HttpGet("student-projects/count")]
        public IActionResult GetTotalStudentProjects()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT COUNT(*) FROM fyp_forms";
                using var command = new MySqlCommand(query, connection);
                int count = Convert.ToInt32(command.ExecuteScalar());

                return Ok(new { totalStudentProjects = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching student project count.", error = ex.Message });
            }
        }

        // GET: /api/GetDashboardStats/funding/total
        [HttpGet("funding/total")]
        public IActionResult GetTotalFunding()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = "SELECT SUM(FundingAmount) FROM randdprojects";
                using var command = new MySqlCommand(query, connection);
                var result = command.ExecuteScalar();
                decimal total = result != DBNull.Value ? Convert.ToDecimal(result) : 0;

                return Ok(new { totalFunding = total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching funding total.", error = ex.Message });
            }
        }
    }
}
