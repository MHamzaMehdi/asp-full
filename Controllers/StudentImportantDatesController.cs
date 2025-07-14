using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentImportantDatesController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/StudentImportantDates/list
        [HttpGet("list")]
        public IActionResult GetImportantDates()
        {
            var events = new List<object>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                var query = @"
                    SELECT Title, EventDate, Time, Type 
                    FROM calendarevents 
                    WHERE Audience IN ('student', 'all') 
                      AND EventDate >= CURDATE()
                    ORDER BY EventDate ASC";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    events.Add(new
                    {
                        title = reader.GetString("Title"),
                        eventDate = reader.GetDateTime("EventDate"),
                        time = DateTime.Today.Add(reader.GetTimeSpan("Time")).ToString("hh:mm tt"),
                        type = reader.GetString("Type")
                    });
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching important dates.", error = ex.Message });
            }
        }
    }
}
