using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacultyImportantDatesController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // GET: /api/FacultyImportantDates/list
        [HttpGet("list")]
        public IActionResult GetImportantDates()
        {
            var events = new List<CalendarEvent>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT Title, EventDate, Time, Type 
                    FROM calendarevents 
                    WHERE Audience IN ('faculty', 'all') 
                    ORDER BY EventDate ASC";

                using var command = new MySqlCommand(query, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    events.Add(new CalendarEvent
                    {
                        Title = reader.GetString("Title"),
                        EventDate = reader.GetDateTime("EventDate"),
                        Time = DateTime.Today.Add(reader.GetTimeSpan("Time")).ToString("hh:mm tt"),
                        Type = reader.GetString("Type")
                    });
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching important dates.", error = ex.Message });
            }
        }

        private class CalendarEvent
        {
            public string Title { get; set; }
            public DateTime EventDate { get; set; }
            public string Time { get; set; }
            public string Type { get; set; }
        }
    }
}
