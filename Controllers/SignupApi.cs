using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignupApi : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // POST: Signup a user
        [HttpPost]
        public IActionResult Signup([FromBody] SignupRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Number) ||
                string.IsNullOrWhiteSpace(request.Role) ||
                string.IsNullOrWhiteSpace(request.Department))
            {
                return BadRequest(new { message = "Invalid signup request. All fields are required." });
            }

            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Check if Email or Phone Number already exists
                    using (var checkCommand = new MySqlCommand(
                        "SELECT email, number FROM user WHERE email = @Email OR number = @Number",
                        connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", request.Email);
                        checkCommand.Parameters.AddWithValue("@Number", request.Number);

                        using (var reader = checkCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["email"].ToString() == request.Email)
                                {
                                    return Conflict(new { message = "Email is already in use. Please use a different email." });
                                }
                                if (reader["number"].ToString() == request.Number)
                                {
                                    return Conflict(new { message = "Phone number is already registered. Please use a different number." });
                                }
                            }
                        }
                    }

                    // ✅ Hash the password using BCrypt
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                    // Insert new user with hashed password, isActive = true and current timestamp
                    using (var insertCommand = new MySqlCommand(
                        "INSERT INTO user (name, email, password, number, role, department, isActive, last_active) " +
                        "VALUES (@Name, @Email, @Password, @Number, @Role, @Department, TRUE, NOW())",
                        connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Name", request.Name);
                        insertCommand.Parameters.AddWithValue("@Email", request.Email);
                        insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                        insertCommand.Parameters.AddWithValue("@Number", request.Number);
                        insertCommand.Parameters.AddWithValue("@Role", request.Role);
                        insertCommand.Parameters.AddWithValue("@Department", request.Department);

                        insertCommand.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "User signed up successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while signing up.", error = ex.Message });
            }
        }

        // DELETE: Delete a user by email
        [HttpDelete("{email}")]
        public IActionResult DeleteUser(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var deleteCommand = new MySqlCommand(
                        "DELETE FROM user WHERE email = @Email",
                        connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@Email", email);

                        int rowsAffected = deleteCommand.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { message = "User not found." });
                        }
                    }
                }

                return Ok(new { message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the user.", error = ex.Message });
            }
        }

        // GET: Get all users with isActive status and last_active
        [HttpGet]
        public IActionResult GetUsers()
        {
            try
            {
                List<UserResponse> users = new List<UserResponse>();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new MySqlCommand(
                        "SELECT name, email, password, number, role, department, " +
                        "CASE WHEN isActive = 1 THEN TRUE ELSE FALSE END as isActive, " +
                        "last_active " +
                        "FROM user",
                        connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new UserResponse
                            {
                                Name = reader["name"].ToString(),
                                Email = reader["email"].ToString(),
                                Password = reader["password"].ToString(),
                                Number = reader["number"].ToString(),
                                Role = reader["role"].ToString(),
                                Department = reader["department"].ToString(),
                                IsActive = reader.GetBoolean("isActive"),
                                LastActive = reader.IsDBNull(reader.GetOrdinal("last_active")) ?
                                    (DateTime?)null :
                                    reader.GetDateTime("last_active")
                            });
                        }
                    }
                }

                if (users.Count == 0)
                {
                    return NotFound(new { message = "No users found." });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching users.", error = ex.Message });
            }
        }

        // PUT: Update user status
        [HttpPut("status/{email}")]
        public IActionResult UpdateUserStatus(string email, [FromBody] StatusUpdateRequest request)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // First check if user exists
                    using (var checkCommand = new MySqlCommand(
                        "SELECT email FROM user WHERE email = @Email",
                        connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", email);
                        if (checkCommand.ExecuteScalar() == null)
                        {
                            return NotFound(new { message = "User not found." });
                        }
                    }

                    // Prevent admin from deactivating themselves
                    if (email == "admin@example.com" && !request.IsActive)
                    {
                        return BadRequest(new { message = "Cannot deactivate primary admin account." });
                    }

                    // Update status and last_active if activating
                    string updateQuery = request.IsActive ?
                        "UPDATE user SET isActive = @IsActive, last_active = NOW() WHERE email = @Email" :
                        "UPDATE user SET isActive = @IsActive WHERE email = @Email";

                    using (var updateCommand = new MySqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@IsActive", request.IsActive);
                        updateCommand.Parameters.AddWithValue("@Email", email);

                        int rowsAffected = updateCommand.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            return StatusCode(500, new { message = "Failed to update user status." });
                        }
                    }
                }

                return Ok(new
                {
                    message = $"User status updated to {(request.IsActive ? "Active" : "Inactive")}",
                    isActive = request.IsActive
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating user status", error = ex.Message });
            }
        }

        // PUT: Update last active time
        [HttpPut("activity/{email}")]
        public IActionResult UpdateLastActive(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new MySqlCommand(
                        "UPDATE user SET last_active = NOW() WHERE email = @Email",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Email", email);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            return NotFound(new { message = "User not found." });
                        }
                    }
                }

                return Ok(new { message = "Last active time updated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating last active time", error = ex.Message });
            }
        }
    }

    public class SignupRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Number { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
    }

    public class UserResponse : SignupRequest
    {
        public bool IsActive { get; set; }
        public DateTime? LastActive { get; set; }
    }

    public class StatusUpdateRequest
    {
        public bool IsActive { get; set; }
    }
}
