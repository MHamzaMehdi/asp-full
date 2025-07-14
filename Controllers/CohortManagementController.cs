using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CohortManagementController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        public CohortManagementController()
        {
            InitializeDatabase().Wait();
        }

        #region CRUD Endpoints

        [HttpPost("create")]
        public async Task<IActionResult> CreateCohort([FromBody] Cohort cohort)
        {
            try
            {
                await EnsureTableExists();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"INSERT INTO CohortManagement 
                                   (Title, Description, StartDate, EndDate, Status)
                                   VALUES (@Title, @Description, @StartDate, @EndDate, @Status);
                                   SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Title", cohort.Title);
                        command.Parameters.AddWithValue("@Description", cohort.Description);
                        command.Parameters.AddWithValue("@StartDate", cohort.StartDate.ToUniversalTime());
                        command.Parameters.AddWithValue("@EndDate", cohort.EndDate.ToUniversalTime());
                        command.Parameters.AddWithValue("@Status", cohort.Status);

                        var newId = await command.ExecuteScalarAsync();
                        return Ok(new
                        {
                            Id = newId,
                            Message = "Cohort created successfully."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error creating cohort: {ex.Message}");
            }
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllCohorts()
        {
            try
            {
                await EnsureTableExists();
                List<Cohort> cohorts = new List<Cohort>();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM CohortManagement ORDER BY StartDate DESC";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cohorts.Add(new Cohort
                            {
                                Id = reader.GetInt32("Id"),
                                Title = reader.GetString("Title"),
                                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ?
                                    null : reader.GetString("Description"),
                                StartDate = reader.GetDateTime("StartDate").ToLocalTime(),
                                EndDate = reader.GetDateTime("EndDate").ToLocalTime(),
                                Status = reader.GetString("Status")
                            });
                        }
                    }
                }

                return Ok(cohorts);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error fetching cohorts: {ex.Message}");
            }
        }

        [HttpGet("getActiveCohort")]
        public async Task<IActionResult> GetActiveCohort()
        {
            try
            {
                await EnsureTableExists();
                Cohort activeCohort = null;

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT * FROM CohortManagement 
                        WHERE StartDate <= @CurrentDate AND EndDate >= @CurrentDate
                        AND Status = 'Active' 
                        LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CurrentDate", DateTime.UtcNow);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                activeCohort = new Cohort
                                {
                                    Id = reader.GetInt32("Id"),
                                    Title = reader.GetString("Title"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                                    StartDate = reader.GetDateTime("StartDate").ToLocalTime(),
                                    EndDate = reader.GetDateTime("EndDate").ToLocalTime(),
                                    Status = reader.GetString("Status")
                                };
                            }
                        }
                    }
                }

                return activeCohort != null ? Ok(activeCohort) : NotFound("No active cohort found.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error fetching active cohort: {ex.Message}");
            }
        }

        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditCohort(int id, [FromBody] Cohort cohort)
        {
            try
            {
                await EnsureTableExists();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE CohortManagement 
                                   SET Title = @Title, 
                                       Description = @Description, 
                                       StartDate = @StartDate, 
                                       EndDate = @EndDate, 
                                       Status = @Status
                                   WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Title", cohort.Title);
                        command.Parameters.AddWithValue("@Description", cohort.Description);
                        command.Parameters.AddWithValue("@StartDate", cohort.StartDate.ToUniversalTime());
                        command.Parameters.AddWithValue("@EndDate", cohort.EndDate.ToUniversalTime());
                        command.Parameters.AddWithValue("@Status", cohort.Status);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound("Cohort not found.");
                        }
                    }
                    return Ok("Cohort updated successfully.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error updating cohort: {ex.Message}");
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteCohort(int id)
        {
            try
            {
                await EnsureTableExists();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"DELETE FROM CohortManagement WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return NotFound("Cohort not found.");
                        }
                    }
                    return Ok("Cohort deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error deleting cohort: {ex.Message}");
            }
        }

        #endregion

        #region Database Initialization

        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize()
        {
            try
            {
                await InitializeDatabase();
                return Ok("Database table initialized successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error initializing database: {ex.Message}");
            }
        }

        private async Task EnsureTableExists()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = 'ORIC' 
                        AND table_name = 'CohortManagement'";

                    using (var command = new MySqlCommand(checkTableQuery, connection))
                    {
                        var result = (long)await command.ExecuteScalarAsync();
                        if (result == 0)
                        {
                            await InitializeDatabase();
                        }
                    }
                }
            }
            catch
            {
                await InitializeDatabase();
            }
        }

        private async Task InitializeDatabase()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS CohortManagement (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(255) NOT NULL,
                        Description TEXT,
                        StartDate DATETIME NOT NULL,
                        EndDate DATETIME NOT NULL,
                        Status VARCHAR(50) NOT NULL DEFAULT 'Active'
                    )";

                using (var command = new MySqlCommand(createTableQuery, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        private string GetCohortStatus(DateTime startDate, DateTime endDate)
        {
            var currentDate = DateTime.UtcNow;
            if (currentDate >= startDate && currentDate <= endDate)
            {
                return "Active";
            }
            return "Inactive";
        }
    }

    public class Cohort
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";  // Default value set to 'Active'
    }
}
