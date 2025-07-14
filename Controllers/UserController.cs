using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        [HttpGet("GetHeaderInfo")]
        public IActionResult GetUserHeaderInfo([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { Message = "Email is required." });
            }

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    var command = new MySqlCommand(
                        "SELECT name, email FROM user WHERE email = @Email",
                        connection);
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var name = reader["name"].ToString();
                            var userEmail = reader["email"].ToString();

                            return Ok(new
                            {
                                Name = name,
                                Email = userEmail
                            });
                        }
                        else
                        {
                            return NotFound(new { Message = "User not found." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while fetching user data.", Error = ex.Message });
            }
        }
    }
}
