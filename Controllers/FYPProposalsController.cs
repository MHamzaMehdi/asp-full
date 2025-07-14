
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FYPProposalsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        private void EnsureTablesExist(MySqlConnection connection)
        {
            string createTablesQuery = @"
                CREATE TABLE IF NOT EXISTS fyp_proposals (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    title TEXT NOT NULL,
                    supervisor TEXT NOT NULL,
                    department TEXT NOT NULL,
                    domain TEXT NOT NULL,
                    deadline DATE NOT NULL,
                    description TEXT NOT NULL,
                    requirements TEXT NOT NULL,
                    teamSize INT NOT NULL,
                    status VARCHAR(50) NOT NULL,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS fyp_applications (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    proposal_id INT NOT NULL,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(100) NOT NULL,
                    status VARCHAR(20) NOT NULL DEFAULT 'pending',
                    applied_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (proposal_id) REFERENCES fyp_proposals(id) ON DELETE CASCADE,
                    UNIQUE KEY unique_application (proposal_id, email)
                );";

            using (var command = new MySqlCommand(createTablesQuery, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        [HttpGet]
        public IActionResult GetAllProposals()
        {
            var proposals = new List<FYPProposal>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string query = @"
                    SELECT 
                        p.id,
                        p.title,
                        p.supervisor,
                        p.department,
                        p.domain,
                        p.deadline,
                        p.description,
                        p.requirements,
                        p.teamSize,
                        p.status,
                        p.created_at,
                        COUNT(CASE WHEN a.name IS NOT NULL THEN 1 END) AS current_applicants,
                        GROUP_CONCAT(DISTINCT CASE 
                            WHEN a.name IS NOT NULL AND a.email IS NOT NULL 
                            THEN JSON_OBJECT('name', a.name, 'email', a.email) 
                            ELSE NULL END) AS applicants,
                        GROUP_CONCAT(DISTINCT CASE 
                            WHEN a.status = 'approved' AND a.name IS NOT NULL AND a.email IS NOT NULL 
                            THEN JSON_OBJECT('name', a.name, 'email', a.email) 
                            ELSE NULL END) AS approved_students
                    FROM fyp_proposals p
                    LEFT JOIN fyp_applications a ON p.id = a.proposal_id
                    GROUP BY p.id
                    ORDER BY p.created_at DESC";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        proposals.Add(MapToProposal(reader));
                    }
                }
            }

            return Ok(proposals);
        }

        [HttpGet("{id}")]
        public IActionResult GetProposalById(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string query = @"
                    SELECT 
                        p.id,
                        p.title,
                        p.supervisor,
                        p.department,
                        p.domain,
                        p.deadline,
                        p.description,
                        p.requirements,
                        p.teamSize,
                        p.status,
                        p.created_at,
                        COUNT(a.id) AS current_applicants,

                        GROUP_CONCAT(DISTINCT 
                            CONCAT(
                                '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                            )
                        ) AS applicants,

                        GROUP_CONCAT(DISTINCT 
                            CASE 
                                WHEN a.status = 'approved' THEN 
                                    CONCAT(
                                        '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                                    )
                                ELSE NULL 
                            END
                        ) AS approved_students

                    FROM fyp_proposals p
                    LEFT JOIN fyp_applications a ON p.id = a.proposal_id
                    WHERE p.id = @Id
                    GROUP BY p.id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(MapToProposal(reader));
                        }
                        return NotFound($"Proposal with ID {id} not found.");
                    }
                }
            }
        }

        [HttpPost]
        public IActionResult CreateProposal([FromBody] FYPProposalCreateDto proposalDto)
        {
            if (proposalDto == null ||
                string.IsNullOrWhiteSpace(proposalDto.Title) ||
                string.IsNullOrWhiteSpace(proposalDto.Supervisor) ||
                string.IsNullOrWhiteSpace(proposalDto.Description))
            {
                return BadRequest("Title, Supervisor, and Description are required fields.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string query = @"
            INSERT INTO fyp_proposals 
            (title, supervisor, department, domain, deadline, description, requirements, teamSize, status) 
            VALUES 
            (@Title, @Supervisor, @Department, @Domain, @Deadline, @Description, @Requirements, @TeamSize, @Status);
            SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", proposalDto.Title);
                    command.Parameters.AddWithValue("@Supervisor", proposalDto.Supervisor);
                    command.Parameters.AddWithValue("@Department", proposalDto.Department ?? "");
                    command.Parameters.AddWithValue("@Domain", proposalDto.Domain ?? "");
                    command.Parameters.AddWithValue("@Deadline", proposalDto.Deadline);
                    command.Parameters.AddWithValue("@Description", proposalDto.Description);
                    command.Parameters.AddWithValue("@Requirements", proposalDto.Requirements ?? "");
                    command.Parameters.AddWithValue("@TeamSize", proposalDto.TeamSize);
                    command.Parameters.AddWithValue("@Status", proposalDto.Status ?? "open");

                    int newId = Convert.ToInt32(command.ExecuteScalar());

                    var createdProposal = MapToProposalWithId(connection, newId);
                    return Ok(createdProposal);
                }
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateProposal(int id, [FromBody] FYPProposalUpdateDto proposalDto)
        {
            if (proposalDto == null || id != proposalDto.Id)
            {
                return BadRequest("Invalid proposal data.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                // Check if proposal exists
                string checkQuery = "SELECT COUNT(*) FROM fyp_proposals WHERE id = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    if ((long)checkCommand.ExecuteScalar() == 0)
                    {
                        return NotFound($"Proposal with ID {id} not found.");
                    }
                }

                // Update proposal
                string updateQuery = @"
            UPDATE fyp_proposals 
            SET title = @Title,
                supervisor = @Supervisor,
                department = @Department,
                domain = @Domain,
                deadline = @Deadline,
                description = @Description,
                requirements = @Requirements,
                teamSize = @TeamSize,
                status = @Status
            WHERE id = @Id";

                using (var command = new MySqlCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Title", proposalDto.Title);
                    command.Parameters.AddWithValue("@Supervisor", proposalDto.Supervisor);
                    command.Parameters.AddWithValue("@Department", proposalDto.Department ?? "");
                    command.Parameters.AddWithValue("@Domain", proposalDto.Domain ?? "");
                    command.Parameters.AddWithValue("@Deadline", proposalDto.Deadline);
                    command.Parameters.AddWithValue("@Description", proposalDto.Description);
                    command.Parameters.AddWithValue("@Requirements", proposalDto.Requirements ?? "");
                    command.Parameters.AddWithValue("@TeamSize", proposalDto.TeamSize);
                    command.Parameters.AddWithValue("@Status", proposalDto.Status ?? "open");

                    int result = command.ExecuteNonQuery();
                    if (result <= 0)
                    {
                        return StatusCode(500, "Failed to update proposal.");
                    }
                }

                // Fetch and return updated proposal
                string fetchQuery = @"
            SELECT 
                p.id,
                p.title,
                p.supervisor,
                p.department,
                p.domain,
                p.deadline,
                p.description,
                p.requirements,
                p.teamSize,
                p.status,
                p.created_at,
                COUNT(a.id) AS current_applicants,
                GROUP_CONCAT(DISTINCT 
                    CONCAT(
                        '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                    )
                ) AS applicants,
                GROUP_CONCAT(DISTINCT 
                    CASE 
                        WHEN a.status = 'approved' THEN 
                            CONCAT(
                                '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                            )
                        ELSE NULL 
                    END
                ) AS approved_students
            FROM fyp_proposals p
            LEFT JOIN fyp_applications a ON p.id = a.proposal_id
            WHERE p.id = @Id
            GROUP BY p.id";

                using (var fetchCommand = new MySqlCommand(fetchQuery, connection))
                {
                    fetchCommand.Parameters.AddWithValue("@Id", id);
                    using (var reader = fetchCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var updatedProposal = MapToProposal(reader);
                            return Ok(updatedProposal);
                        }
                    }
                }

                return StatusCode(500, "Failed to fetch updated proposal.");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProposal(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string checkQuery = "SELECT COUNT(*) FROM fyp_proposals WHERE id = @Id";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Id", id);
                    if ((long)checkCommand.ExecuteScalar() == 0)
                    {
                        return NotFound($"Proposal with ID {id} not found.");
                    }
                }

                string deleteQuery = "DELETE FROM fyp_proposals WHERE id = @Id";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    int result = command.ExecuteNonQuery();
                    return result > 0 ? Ok(new { Message = "Proposal deleted successfully." }) : StatusCode(500, "Failed to delete proposal.");
                }
            }
        }

        [HttpPost("{id}/apply")]
        public IActionResult ApplyForProposal(int id, [FromBody] ApplicationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name and Email are required.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check proposal exists and has capacity
                        string checkQuery = @"
                            SELECT p.teamSize, 
                                   COUNT(a.id) AS current_applicants
                            FROM fyp_proposals p
                            LEFT JOIN fyp_applications a ON p.id = a.proposal_id AND a.status = 'approved'
                            WHERE p.id = @Id
                            GROUP BY p.id";

                        using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@Id", id);
                            using (var reader = checkCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    transaction.Rollback();
                                    return NotFound($"Proposal with ID {id} not found.");
                                }

                                int teamSize = Convert.ToInt32(reader["teamSize"]);
                                int currentApplicants = reader["current_applicants"] != DBNull.Value ? Convert.ToInt32(reader["current_applicants"]) : 0;

                                if (currentApplicants >= teamSize)
                                {
                                    transaction.Rollback();
                                    return BadRequest("This proposal has reached maximum team size.");
                                }
                            }
                        }

                        // Check if already applied
                        string checkApplicationQuery = @"
                            SELECT id FROM fyp_applications 
                            WHERE proposal_id = @Id AND email = @Email";

                        using (var checkAppCommand = new MySqlCommand(checkApplicationQuery, connection, transaction))
                        {
                            checkAppCommand.Parameters.AddWithValue("@Id", id);
                            checkAppCommand.Parameters.AddWithValue("@Email", request.Email);

                            if (checkAppCommand.ExecuteScalar() != null)
                            {
                                transaction.Rollback();
                                return BadRequest("You have already applied to this proposal.");
                            }
                        }

                        // Apply for proposal
                        string applyQuery = @"
                            INSERT INTO fyp_applications 
                            (proposal_id, name, email) 
                            VALUES (@Id, @Name, @Email)";

                        using (var applyCommand = new MySqlCommand(applyQuery, connection, transaction))
                        {
                            applyCommand.Parameters.AddWithValue("@Id", id);
                            applyCommand.Parameters.AddWithValue("@Name", request.Name);
                            applyCommand.Parameters.AddWithValue("@Email", request.Email);

                            int result = applyCommand.ExecuteNonQuery();
                            if (result <= 0)
                            {
                                transaction.Rollback();
                                return StatusCode(500, "Failed to submit application.");
                            }
                        }

                        transaction.Commit();
                        return Ok(new { Message = "Application submitted successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"An error occurred: {ex.Message}");
                    }
                }
            }
        }

        [HttpPost("{id}/withdraw")]
        public IActionResult WithdrawApplication(int id, [FromBody] ApplicationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string query = @"
                    DELETE FROM fyp_applications 
                    WHERE proposal_id = @Id AND email = @Email";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Email", request.Email);

                    int result = command.ExecuteNonQuery();
                    return result > 0 ? Ok(new { Message = "Application withdrawn successfully." }) : NotFound("Application not found.");
                }
            }
        }

        [HttpPost("{id}/reject")]
        public IActionResult RejectApplication(int id, [FromBody] ApplicationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                string query = @"
                    DELETE FROM fyp_applications 
                    WHERE proposal_id = @Id AND email = @Email";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@Email", request.Email);

                    int result = command.ExecuteNonQuery();
                    return result > 0
                        ? Ok(new { Message = "Application rejected successfully." })
                        : NotFound("Application not found.");
                }
            }
        }


        [HttpPost("{id}/approve")]
        public IActionResult ApproveApplication(int id, [FromBody] ApplicationRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTablesExist(connection);

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string checkQuery = @"
                            SELECT p.teamSize,
                                   COUNT(CASE WHEN a.status = 'approved' THEN 1 ELSE NULL END) AS approved_count
                            FROM fyp_proposals p
                            LEFT JOIN fyp_applications a ON p.id = a.proposal_id
                            WHERE p.id = @Id
                            GROUP BY p.id";

                        using (var checkCommand = new MySqlCommand(checkQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@Id", id);
                            using (var reader = checkCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    transaction.Rollback();
                                    return NotFound($"Proposal with ID {id} not found.");
                                }

                                int teamSize = Convert.ToInt32(reader["teamSize"]);
                                int approvedCount = reader["approved_count"] != DBNull.Value ? Convert.ToInt32(reader["approved_count"]) : 0;

                                if (approvedCount >= teamSize)
                                {
                                    transaction.Rollback();
                                    return BadRequest("This proposal has reached maximum team size.");
                                }
                            }
                        }

                        string approveQuery = @"
                            UPDATE fyp_applications 
                            SET status = 'approved' 
                            WHERE proposal_id = @Id AND email = @Email";

                        using (var approveCommand = new MySqlCommand(approveQuery, connection, transaction))
                        {
                            approveCommand.Parameters.AddWithValue("@Id", id);
                            approveCommand.Parameters.AddWithValue("@Email", request.Email);

                            int result = approveCommand.ExecuteNonQuery();
                            if (result <= 0)
                            {
                                transaction.Rollback();
                                return NotFound("Application not found or already approved.");
                            }
                        }

                        transaction.Commit();
                        return Ok(new { Message = "Application approved successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"An error occurred: {ex.Message}");
                    }
                }
            }
        }

        private FYPProposal MapToProposal(MySqlDataReader reader)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string rawApplicants = reader["applicants"]?.ToString();
            string rawApproved = reader["approved_students"]?.ToString();

            string FixConcatJson(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return "[]";
                return "[" + input.Replace("}{", "},{") + "]";
            }

            return new FYPProposal
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString(),
                Supervisor = reader["supervisor"].ToString(),
                Department = reader["department"].ToString(),
                Domain = reader["domain"].ToString(),
                Deadline = Convert.ToDateTime(reader["deadline"]),
                Description = reader["description"].ToString(),
                Requirements = reader["requirements"].ToString(),
                TeamSize = Convert.ToInt32(reader["teamSize"]),
                Status = reader["status"].ToString(),
                CreatedAt = reader["created_at"] != DBNull.Value ? Convert.ToDateTime(reader["created_at"]) : DateTime.MinValue,
                CurrentApplicants = reader["current_applicants"] != DBNull.Value ? Convert.ToInt32(reader["current_applicants"]) : 0,
                ApplicantDetails = JsonSerializer.Deserialize<List<StudentInfo>>(FixConcatJson(rawApplicants), options),
                ApprovedStudentDetails = JsonSerializer.Deserialize<List<StudentInfo>>(FixConcatJson(rawApproved), options)
            };
        }



        private List<StudentInfo> ParseStudentInfo(string jsonArray)
        {
            var students = new List<StudentInfo>();
            if (string.IsNullOrEmpty(jsonArray)) return students;

            try
            {
                var entries = jsonArray.Split(new[] { "}{" }, StringSplitOptions.None);
                foreach (var entry in entries)
                {
                    var cleanEntry = entry.Trim('{', '}');
                    var parts = cleanEntry.Split(new[] { "\", \"" }, StringSplitOptions.None);
                    var name = parts[0].Split(':')[1].Trim('"');
                    var email = parts[1].Split(':')[1].Trim('"');
                    students.Add(new StudentInfo { Name = name, Email = email });
                }
            }
            catch (Exception ex)
            {
                // Log the error details including the problematic jsonArray
                Console.WriteLine($"Error parsing student info: {ex.Message}");
                Console.WriteLine($"Problematic JSON array: {jsonArray}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Return empty list as fallback
                return new List<StudentInfo>();
            }

            return students;
        }

        private FYPProposal MapToProposalWithId(MySqlConnection connection, int id)
        {
            string query = @"
                SELECT 
                    p.id,
                    p.title,
                    p.supervisor,
                    p.department,
                    p.domain,
                    p.deadline,
                    p.description,
                    p.requirements,
                    p.teamSize,
                    p.status,
                    p.created_at,
                    COUNT(a.id) AS current_applicants,
                    GROUP_CONCAT(DISTINCT 
                        CONCAT(
                            '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                        )
                    ) AS applicants,
                    GROUP_CONCAT(DISTINCT 
                        CASE WHEN a.status = 'approved' THEN 
                            CONCAT(
                                '{""name"": ""', a.name, '"", ""email"": ""', a.email, '""}'
                            )
                        ELSE NULL END
                    ) AS approved_students
                FROM fyp_proposals p
                LEFT JOIN fyp_applications a ON p.id = a.proposal_id
                WHERE p.id = @Id
                GROUP BY p.id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = command.ExecuteReader();
            return reader.Read() ? MapToProposal(reader) : null;
        }
    }

    public class FYPProposal
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Supervisor { get; set; }
        public string Department { get; set; }
        public string Domain { get; set; }
        public DateTime Deadline { get; set; }
        public string Description { get; set; }
        public string Requirements { get; set; }
        public int TeamSize { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CurrentApplicants { get; set; }
        public List<StudentInfo> ApplicantDetails { get; set; } = new();
        public List<StudentInfo> ApprovedStudentDetails { get; set; } = new();
    }

    public class FYPProposalCreateDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("supervisor")]
        public string Supervisor { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("deadline")]
        public DateTime Deadline { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("requirements")]
        public string Requirements { get; set; }

        [JsonPropertyName("teamSize")]
        public int TeamSize { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class FYPProposalUpdateDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("supervisor")]
        public string Supervisor { get; set; }

        [JsonPropertyName("department")]
        public string Department { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("deadline")]
        public DateTime Deadline { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("requirements")]
        public string Requirements { get; set; }

        [JsonPropertyName("teamSize")]
        public int TeamSize { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class ApplicationRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class StudentInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}