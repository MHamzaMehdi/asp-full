using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        private void EnsureTicketsTableExists(MySqlConnection connection)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS tickets (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    subject TEXT NOT NULL,
                    details TEXT NOT NULL,
                    priority VARCHAR(100) NOT NULL,
                    createdAt DATETIME NOT NULL,
                    userEmail VARCHAR(255) NOT NULL,
                    userName VARCHAR(255) NOT NULL,
                    userRole VARCHAR(50) NOT NULL,
                    status ENUM('Open', 'In Progress', 'Resolved', 'Closed') NOT NULL DEFAULT 'Open',
                    response TEXT NULL
                );";

            MySqlCommand command = new MySqlCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        // Get all tickets
        [HttpGet]
        public IActionResult GetAllTickets()
        {
            List<Ticket> ticketList = new List<Ticket>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTicketsTableExists(connection);

                string query = "SELECT * FROM tickets ORDER BY createdAt DESC";
                MySqlCommand command = new MySqlCommand(query, connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ticketList.Add(new Ticket
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Subject = reader["subject"].ToString(),
                            Details = reader["details"].ToString(),
                            Priority = reader["priority"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["createdAt"]),
                            UserEmail = reader["userEmail"].ToString(),
                            UserName = reader["userName"].ToString(),
                            UserRole = reader["userRole"].ToString(),
                            Status = reader["status"].ToString(),
                            Response = reader["response"]?.ToString()  // Fetch response from database (may be null initially)
                        });
                    }
                }
            }

            return Ok(ticketList);
        }

        // Create a new ticket
        [HttpPost]
        public IActionResult CreateTicket([FromBody] Ticket ticket)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(ticket.Subject) || string.IsNullOrWhiteSpace(ticket.Details))
                return BadRequest("Subject and Details are required.");

            ticket.CreatedAt = DateTime.UtcNow;
            ticket.Response = "Awaiting for response";  // Default response when a ticket is created

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTicketsTableExists(connection);

                string query = @"
                    INSERT INTO tickets (subject, details, priority, createdAt, userEmail, userName, userRole, status, response) 
                    VALUES (@Subject, @Details, @Priority, @CreatedAt, @UserEmail, @UserName, @UserRole, @Status, @Response)";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Subject", ticket.Subject);
                command.Parameters.AddWithValue("@Details", ticket.Details);
                command.Parameters.AddWithValue("@Priority", ticket.Priority ?? "medium");
                command.Parameters.AddWithValue("@CreatedAt", ticket.CreatedAt);
                command.Parameters.AddWithValue("@UserEmail", ticket.UserEmail ?? "");
                command.Parameters.AddWithValue("@UserName", ticket.UserName ?? "");
                command.Parameters.AddWithValue("@UserRole", ticket.UserRole ?? "");
                command.Parameters.AddWithValue("@Status", ticket.Status ?? "Open");
                command.Parameters.AddWithValue("@Response", ticket.Response);  // Set initial response

                int result = command.ExecuteNonQuery();
                return result > 0 ? Ok(new { Message = "Ticket submitted successfully." }) : StatusCode(500, "Failed to submit ticket.");
            }
        }

        // Update an existing ticket (admin updates response and status)
        [HttpPut("{id}")]
        public IActionResult UpdateTicket(int id, [FromBody] Ticket ticket)
        {
            if (ticket == null || id <= 0)
                return BadRequest("Invalid ticket data.");

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureTicketsTableExists(connection);

                string query = @"
                    UPDATE tickets 
                    SET status = @Status,
                        response = @Response 
                    WHERE id = @Id";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Status", ticket.Status ?? "Open");
                command.Parameters.AddWithValue("@Response", ticket.Response ?? "");  // Admin's response
                command.Parameters.AddWithValue("@Id", id);

                int result = command.ExecuteNonQuery();
                return result > 0 ? Ok(new { Message = "Ticket updated successfully." }) : NotFound(new { Message = "Ticket not found or no changes made." });
            }
        }
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Details { get; set; }
        public string Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }  // Admin's response (nullable)
    }
}
