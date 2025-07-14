using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Testing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportGenerationController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        [HttpGet("faculty-proposals/report")]
        public IActionResult GetFacultyProposalsReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                List<FacultyProposal> proposals = new();
                int total = 0, acknowledged = 0, pending = 0, rejected = 0;

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT 
                        Id,
                        ProposalTitle AS Title,
                        FacultyName,
                        Department,
                        SubmissionDate,
                        Status
                    FROM faculty_proposals
                    WHERE (@startDate IS NULL OR SubmissionDate >= @startDate)
                      AND (@endDate IS NULL OR SubmissionDate <= @endDate)
                    ORDER BY SubmissionDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@startDate", startDate?.Date ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@endDate", endDate?.Date ?? (object)DBNull.Value);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var status = reader["Status"].ToString();
                    switch (status)
                    {
                        case "Acknowledged": acknowledged++; break;
                        case "Rejected": rejected++; break;
                        default: pending++; break;
                    }
                    total++;

                    proposals.Add(new FacultyProposal
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString(),
                        FacultyName = reader["FacultyName"].ToString(),
                        Department = reader["Department"].ToString(),
                        SubmissionDate = Convert.ToDateTime(reader["SubmissionDate"]),
                        Status = status
                    });
                }

                var response = new
                {
                    TotalProposals = total,
                    AcknowledgedProposals = acknowledged,
                    RejectedProposals = rejected,
                    PendingProposals = pending,
                    Proposals = proposals
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("departmental-impact/report")]
        public IActionResult GetDepartmentalImpactReport(DateTime? startDate = null, DateTime? endDate = null, string department = null)
        {
            try
            {
                List<FacultyProposal> proposals = new();
                int total = 0, acknowledged = 0, pending = 0, rejected = 0;

                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT 
                        Id,
                        ProposalTitle AS Title,
                        FacultyName,
                        Department,
                        SubmissionDate,
                        Status
                    FROM faculty_proposals
                    WHERE (@startDate IS NULL OR SubmissionDate >= @startDate)
                      AND (@endDate IS NULL OR SubmissionDate <= @endDate)
                      AND (@department IS NULL OR Department = @department)
                    ORDER BY SubmissionDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@startDate", startDate?.Date ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@endDate", endDate?.Date ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@department", string.IsNullOrEmpty(department) ? (object)DBNull.Value : department);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var status = reader["Status"].ToString();
                    switch (status)
                    {
                        case "Acknowledged": acknowledged++; break;
                        case "Rejected": rejected++; break;
                        default: pending++; break;
                    }
                    total++;

                    proposals.Add(new FacultyProposal
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString(),
                        FacultyName = reader["FacultyName"].ToString(),
                        Department = reader["Department"].ToString(),
                        SubmissionDate = Convert.ToDateTime(reader["SubmissionDate"]),
                        Status = status
                    });
                }

                var response = new
                {
                    TotalProposals = total,
                    AcknowledgedProposals = acknowledged,
                    RejectedProposals = rejected,
                    PendingProposals = pending,
                    Proposals = proposals
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("rnd-projects/funding-report")]
        public IActionResult GetFundingAllocationReport(DateTime? startDate = null, DateTime? endDate = null, string fundingAgency = null)
        {
            try
            {
                // First verify table exists
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                // Check if table exists
                var tableCheckCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'ORIC' AND table_name = 'rnd_projects'",
                    connection);
                var exists = Convert.ToInt32(tableCheckCmd.ExecuteScalar()) > 0;

                if (!exists)
                {
                    return StatusCode(500, "Required database table 'rnd_projects' not found");
                }

                List<RNDProject> projects = new();
                int total = 0, acknowledged = 0, pending = 0, rejected = 0;

                string query = @"
                    SELECT 
                        Id,
                        Title,
                        COALESCE(PI, 'Not Assigned') as FacultyName,
                        COALESCE(Department, 'Not Specified') as Department,
                        COALESCE(StartDate, CURRENT_DATE) as SubmissionDate,
                        COALESCE(Status, 'Pending') as Status,
                        COALESCE(FundingAgency, 'Not Specified') as FundingAgency,
                        COALESCE(FundingAmount, 0) as FundingAmount
                    FROM rnd_projects
                    WHERE (@startDate IS NULL OR StartDate >= @startDate)
                      AND (@endDate IS NULL OR StartDate <= @endDate)
                      AND (@fundingAgency IS NULL OR @fundingAgency = 'all' 
                           OR FundingAgency = @fundingAgency)
                    ORDER BY StartDate DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@startDate", startDate?.Date ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@endDate", endDate?.Date ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@fundingAgency",
                    string.IsNullOrEmpty(fundingAgency) || fundingAgency == "all"
                        ? (object)DBNull.Value
                        : fundingAgency);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var status = (reader["Status"] ?? "Pending").ToString();
                    switch (status.ToLower())
                    {
                        case "completed":
                        case "acknowledged":
                            acknowledged++;
                            break;
                        case "rejected":
                            rejected++;
                            break;
                        default:
                            pending++;
                            break;
                    }
                    total++;

                    projects.Add(new RNDProject
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString(),
                        FacultyName = reader["FacultyName"].ToString(),
                        Department = reader["Department"].ToString(),
                        SubmissionDate = Convert.ToDateTime(reader["SubmissionDate"]),
                        Status = status,
                        FundingAgency = reader["FundingAgency"].ToString(),
                        FundingAmount = reader["FundingAmount"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["FundingAmount"])
                    });
                }

                return Ok(new
                {
                    Total = total,
                    Acknowledged = acknowledged,
                    Rejected = rejected,
                    Pending = pending,
                    Items = projects
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class FacultyProposal
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string FacultyName { get; set; }
            public string Department { get; set; }
            public DateTime SubmissionDate { get; set; }
            public string Status { get; set; }
        }

        public class RNDProject
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string FacultyName { get; set; }
            public string Department { get; set; }
            public DateTime SubmissionDate { get; set; }
            public string Status { get; set; }
            public string FundingAgency { get; set; }
            public decimal FundingAmount { get; set; }
        }
    }
}
