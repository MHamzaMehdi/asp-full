using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginApi : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";
        private readonly IConfiguration _config;

        public LoginApi(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Invalid login request.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var command = new MySqlCommand(
                    "SELECT id, name, role, department, password, isActive FROM user WHERE email = @Email",
                    connection);
                command.Parameters.AddWithValue("@Email", request.Email);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        bool isActive = Convert.ToBoolean(reader["isActive"]);
                        if (!isActive)
                        {
                            return Unauthorized(new { message = "Account is inactive. Please contact admin." });
                        }

                        int userId = Convert.ToInt32(reader["id"]);
                        string name = reader["name"].ToString();
                        string role = reader["role"].ToString();
                        string department = reader["department"].ToString();
                        string hashedPassword = reader["password"].ToString();

                        // 🔐 Verify password
                        if (!BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword))
                        {
                            return Unauthorized(new { message = "Invalid email or password." });
                        }

                        reader.Close();

                        // Update last active
                        var updateCommand = new MySqlCommand(
                            "UPDATE user SET last_active = @LastActive WHERE id = @UserId", connection);
                        updateCommand.Parameters.AddWithValue("@LastActive", DateTime.Now);
                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                        updateCommand.ExecuteNonQuery();

                        // ✅ Generate JWT token
                        string token = GenerateJwtToken(userId, request.Email, role);

                        return Ok(new
                        {
                            message = "Login successful",
                            token,
                            name,
                            email = request.Email,
                            role,
                            department
                        });
                    }
                    else
                    {
                        return Unauthorized(new { message = "Invalid email or password." });
                    }
                }
            }
        }

        // 🔐 Token generation using appsettings key
        private string GenerateJwtToken(int userId, string email, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
