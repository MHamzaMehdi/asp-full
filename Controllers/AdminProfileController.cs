using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminProfileController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";
        private readonly IWebHostEnvironment _env;

        public AdminProfileController(IWebHostEnvironment env)
        {
            _env = env;
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"
                    CREATE TABLE IF NOT EXISTS AdminProfiles (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        FullName VARCHAR(255) NOT NULL,
                        Email VARCHAR(255) NOT NULL,
                        Department VARCHAR(255),
                        Role VARCHAR(100),
                        Phone VARCHAR(50),
                        OfficeLocation VARCHAR(255),
                        Biography TEXT,
                        ProfileImageUrl TEXT
                    )";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // ✅ POST: Add New Admin Profile
        [HttpPost("add")]
        public async Task<IActionResult> AddProfile([FromForm] AdminProfileRequest request)
        {
            if (string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email))
                return BadRequest("FullName and Email are required.");

            string imageUrl = null;

            if (request.ProfileImage != null && request.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "profile_images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{request.ProfileImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ProfileImage.CopyToAsync(fileStream);
                }

                imageUrl = $"/profile_images/{uniqueFileName}";
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var insertQuery = @"
                    INSERT INTO AdminProfiles 
                    (FullName, Email, Department, Role, Phone, OfficeLocation, Biography, ProfileImageUrl)
                    VALUES 
                    (@FullName, @Email, @Department, @Role, @Phone, @OfficeLocation, @Biography, @ProfileImageUrl)";

                using (var cmd = new MySqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@FullName", request.FullName);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Department", request.Department ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", request.Role ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", request.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OfficeLocation", request.OfficeLocation ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Biography", request.Biography ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProfileImageUrl", imageUrl);

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Admin profile created successfully." });
        }

        // ✅ PUT: Update Admin Profile
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditProfile(int id, [FromForm] AdminProfileRequest request)
        {
            if (string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email))
                return BadRequest("FullName and Email are required.");

            string imageUrl = null;

            if (request.ProfileImage != null && request.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "profile_images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{request.ProfileImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ProfileImage.CopyToAsync(fileStream);
                }

                imageUrl = $"/profile_images/{uniqueFileName}";
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var checkQuery = "SELECT COUNT(*) FROM AdminProfiles WHERE Id = @Id";
                using (var checkCmd = new MySqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Id", id);
                    var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    if (count == 0)
                        return NotFound("Admin profile not found.");
                }

                var updateQuery = @"
                    UPDATE AdminProfiles 
                    SET FullName = @FullName, Email = @Email, Department = @Department, 
                        Role = @Role, Phone = @Phone, OfficeLocation = @OfficeLocation, 
                        Biography = @Biography" + (imageUrl != null ? ", ProfileImageUrl = @ProfileImageUrl" : "") + @"
                    WHERE Id = @Id";

                using (var cmd = new MySqlCommand(updateQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@FullName", request.FullName);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Department", request.Department ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", request.Role ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Phone", request.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OfficeLocation", request.OfficeLocation ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Biography", request.Biography ?? (object)DBNull.Value);

                    if (imageUrl != null)
                        cmd.Parameters.AddWithValue("@ProfileImageUrl", imageUrl);

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Admin profile updated successfully." });
        }
    }

    // ✅ Request Model
    public class AdminProfileRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
        public string Phone { get; set; }
        public string OfficeLocation { get; set; }
        public string Biography { get; set; }
        public IFormFile ProfileImage { get; set; }
    }
}
