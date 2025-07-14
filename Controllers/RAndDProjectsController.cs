using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RAndDProjectsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        public RAndDProjectsController()
        {
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string createProjectsTable = @"
                    CREATE TABLE IF NOT EXISTS randdprojects (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(255) NOT NULL,
                        Description TEXT,
                        FundingAgency VARCHAR(255) NOT NULL,
                        FundingAmount DECIMAL(15,2) NOT NULL,
                        StartDate DATE NOT NULL,
                        EndDate DATE NOT NULL,
                        PI VARCHAR(255) NOT NULL,
                        Department VARCHAR(255) NOT NULL
                    );";

                string createDeliverablesTable = @"
                    CREATE TABLE IF NOT EXISTS deliverables (
                        Id INT AUTO_INCREMENT PRIMARY KEY,
                        ProjectId INT NOT NULL,
                        Title VARCHAR(255) NOT NULL,
                        DueDate DATE NOT NULL,
                        FundingReleased DECIMAL(15,2) DEFAULT 0,
                        Status ENUM('Not Started', 'Pending', 'In Progress', 'Completed') NOT NULL DEFAULT 'Not Started',
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (ProjectId) REFERENCES randdprojects(Id) ON DELETE CASCADE
                    );";

                using var command = new MySqlCommand(createProjectsTable + createDeliverablesTable, connection);
                command.ExecuteNonQuery();
            }
        }

        // --------------------- Project Endpoints ---------------------

        [HttpPost("add")]
        public IActionResult AddProject([FromBody] RAndDProjectRequest request)
        {
            if (request == null) return BadRequest(new { message = "Invalid request." });

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = @"INSERT INTO randdprojects 
                            (Title, FundingAgency, FundingAmount, StartDate, EndDate, PI, Department, Description) 
                            VALUES 
                            (@Title, @FundingAgency, @FundingAmount, @StartDate, @EndDate, @PI, @Department, @Description)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@FundingAgency", request.FundingAgency);
            command.Parameters.AddWithValue("@FundingAmount", request.FundingAmount);
            command.Parameters.AddWithValue("@StartDate", request.StartDate);
            command.Parameters.AddWithValue("@EndDate", request.EndDate);
            command.Parameters.AddWithValue("@PI", request.PI);
            command.Parameters.AddWithValue("@Department", request.Department);
            command.Parameters.AddWithValue("@Description", request.Description);
            command.ExecuteNonQuery();

            return Ok(new { message = "Project added successfully!" });
        }

        [HttpGet("all")]
        public IActionResult GetAllProjects()
        {
            List<RAndDProjectResponse> projects = new();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = "SELECT * FROM randdprojects";
            using var command = new MySqlCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                projects.Add(new RAndDProjectResponse
                {
                    Id = reader.GetInt32("Id"),
                    Title = reader.GetString("Title"),
                    FundingAgency = reader.GetString("FundingAgency"),
                    FundingAmount = reader.GetDecimal("FundingAmount"),
                    StartDate = reader.GetDateTime("StartDate"),
                    EndDate = reader.GetDateTime("EndDate"),
                    PI = reader.GetString("PI"),
                    Department = reader.GetString("Department"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description")
                });
            }

            return Ok(projects);
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteProject(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string checkQuery = "SELECT COUNT(*) FROM randdprojects WHERE Id = @Id";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@Id", id);
            var count = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (count == 0) return NotFound(new { message = "Project not found." });

            string deleteQuery = "DELETE FROM randdprojects WHERE Id = @Id";
            using var deleteCommand = new MySqlCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@Id", id);
            deleteCommand.ExecuteNonQuery();

            return Ok(new { message = "Project deleted successfully!" });
        }

        [HttpPut("update/{id}")]
        public IActionResult UpdateProject(int id, [FromBody] RAndDProjectRequest request)
        {
            if (request == null) return BadRequest(new { message = "Invalid request." });

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string checkQuery = "SELECT COUNT(*) FROM randdprojects WHERE Id = @Id";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@Id", id);
            if (Convert.ToInt32(checkCommand.ExecuteScalar()) == 0)
                return NotFound(new { message = "Project not found." });

            string updateQuery = @"
                UPDATE randdprojects 
                SET Title = @Title, FundingAgency = @FundingAgency, FundingAmount = @FundingAmount, 
                    StartDate = @StartDate, EndDate = @EndDate, PI = @PI, Department = @Department, 
                    Description = @Description
                WHERE Id = @Id";

            using var updateCommand = new MySqlCommand(updateQuery, connection);
            updateCommand.Parameters.AddWithValue("@Id", id);
            updateCommand.Parameters.AddWithValue("@Title", request.Title);
            updateCommand.Parameters.AddWithValue("@FundingAgency", request.FundingAgency);
            updateCommand.Parameters.AddWithValue("@FundingAmount", request.FundingAmount);
            updateCommand.Parameters.AddWithValue("@StartDate", request.StartDate);
            updateCommand.Parameters.AddWithValue("@EndDate", request.EndDate);
            updateCommand.Parameters.AddWithValue("@PI", request.PI);
            updateCommand.Parameters.AddWithValue("@Department", request.Department);
            updateCommand.Parameters.AddWithValue("@Description", request.Description);
            updateCommand.ExecuteNonQuery();

            return Ok(new { message = "Project updated successfully!" });
        }

        // --------------------- Deliverables Endpoints ---------------------

        [HttpPost("{projectId}/add-deliverable")]
        public IActionResult AddDeliverable(int projectId, [FromBody] DeliverableRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Invalid deliverable data.");

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string insertQuery = @"
                INSERT INTO deliverables (ProjectId, Title, DueDate, FundingReleased, Status)
                VALUES (@ProjectId, @Title, @DueDate, @FundingReleased, @Status);";

            using var command = new MySqlCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@ProjectId", projectId);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@DueDate", request.DueDate);
            command.Parameters.AddWithValue("@FundingReleased", request.FundingReleased);
            command.Parameters.AddWithValue("@Status", request.Status);
            command.ExecuteNonQuery();

            return Ok(new { message = "Deliverable added successfully" });
        }

        [HttpGet("{projectId}/deliverables")]
        public IActionResult GetDeliverables(int projectId)
        {
            List<DeliverableResponse> deliverables = new();

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();
            string query = "SELECT * FROM deliverables WHERE ProjectId = @ProjectId ORDER BY DueDate ASC";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProjectId", projectId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                deliverables.Add(new DeliverableResponse
                {
                    Id = reader.GetInt32("Id"),
                    ProjectId = reader.GetInt32("ProjectId"),
                    Title = reader.GetString("Title"),
                    DueDate = reader.GetDateTime("DueDate"),
                    FundingReleased = reader.GetDecimal("FundingReleased"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }

            return Ok(deliverables);
        }

        [HttpPut("update-deliverable/{id}")]
        public IActionResult UpdateDeliverable(int id, [FromBody] DeliverableRequest request)
        {
            if (request == null) return BadRequest("Invalid data.");

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE deliverables
                SET Title = @Title,
                    DueDate = @DueDate,
                    FundingReleased = @FundingReleased,
                    Status = @Status
                WHERE Id = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Title", request.Title);
            command.Parameters.AddWithValue("@DueDate", request.DueDate);
            command.Parameters.AddWithValue("@FundingReleased", request.FundingReleased);
            command.Parameters.AddWithValue("@Status", request.Status);
            int rowsAffected = command.ExecuteNonQuery();

            return rowsAffected > 0
                ? Ok(new { message = "Deliverable updated successfully" })
                : NotFound("Deliverable not found.");
        }

        [HttpDelete("delete-deliverable/{projectId}/{deliverableId}")]
        public IActionResult DeleteDeliverable(int projectId, int deliverableId)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = "DELETE FROM deliverables WHERE Id = @Id AND ProjectId = @ProjectId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", deliverableId);
            command.Parameters.AddWithValue("@ProjectId", projectId);

            int result = command.ExecuteNonQuery();
            return result > 0
                ? Ok(new { message = "Deliverable deleted successfully" })
                : NotFound("Deliverable not found.");
        }
    }

    // ----------------- Models -----------------

    public class RAndDProjectRequest
    {
        public string Title { get; set; }
        public string FundingAgency { get; set; }
        public decimal FundingAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PI { get; set; }
        public string Department { get; set; }
        public string Description { get; set; } // NEW FIELD
    }

    public class RAndDProjectResponse : RAndDProjectRequest
    {
        public int Id { get; set; }
    }

    public class DeliverableRequest
    {
        public string Title { get; set; }
        public DateTime DueDate { get; set; }
        public decimal FundingReleased { get; set; }
        public string Status { get; set; }
    }

    public class DeliverableResponse : DeliverableRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
