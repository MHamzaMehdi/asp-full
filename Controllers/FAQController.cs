using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FAQController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        private void EnsureFAQTableExists(MySqlConnection connection)
        {
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS faq (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    question TEXT NOT NULL,
                    answer TEXT NOT NULL,
                    audience VARCHAR(255) NOT NULL
                );";
            MySqlCommand command = new MySqlCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }

        [HttpGet]
        public IActionResult GetAllFAQs()
        {
            List<FAQ> faqList = new List<FAQ>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureFAQTableExists(connection);

                string query = "SELECT * FROM faq";
                MySqlCommand command = new MySqlCommand(query, connection);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        faqList.Add(new FAQ
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Question = reader["question"].ToString(),
                            Answer = reader["answer"].ToString(),
                            Audience = reader["audience"].ToString()
                        });
                    }
                }
            }

            return Ok(faqList);
        }

        [HttpPost]
        public IActionResult CreateFAQ([FromBody] FAQ faq)
        {
            if (faq == null || string.IsNullOrEmpty(faq.Question))
                return BadRequest("Invalid data.");

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureFAQTableExists(connection);

                string query = "INSERT INTO faq (question, answer, audience) VALUES (@Question, @Answer, @Audience)";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Question", faq.Question);
                command.Parameters.AddWithValue("@Answer", faq.Answer);
                command.Parameters.AddWithValue("@Audience", faq.Audience);

                int result = command.ExecuteNonQuery();
                return result > 0 ? Ok(new { Message = "FAQ created successfully." }) : StatusCode(500, "Failed to create FAQ.");
            }
        }

        [HttpPut("{id}")]
        public IActionResult UpdateFAQ(int id, [FromBody] FAQ faq)
        {
            if (faq == null || id <= 0)
                return BadRequest("Invalid data.");

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureFAQTableExists(connection);

                string query = "UPDATE faq SET question = @Question, answer = @Answer, audience = @Audience WHERE id = @Id";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Question", faq.Question);
                command.Parameters.AddWithValue("@Answer", faq.Answer);
                command.Parameters.AddWithValue("@Audience", faq.Audience);
                command.Parameters.AddWithValue("@Id", id);

                int result = command.ExecuteNonQuery();
                return result > 0 ? Ok(new { Message = "FAQ updated successfully." }) : NotFound(new { Message = "FAQ not found." });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteFAQ(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                EnsureFAQTableExists(connection);

                string query = "DELETE FROM faq WHERE id = @Id";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);

                int result = command.ExecuteNonQuery();
                return result > 0 ? Ok(new { Message = "FAQ deleted successfully." }) : NotFound(new { Message = "FAQ not found." });
            }
        }
    }

    public class FAQ
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Audience { get; set; }
    }
}
