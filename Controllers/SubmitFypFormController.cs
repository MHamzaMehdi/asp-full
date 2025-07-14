// SubmitFypFormController.cs
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Linq;
using System.Text.Json.Serialization;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubmitFypFormController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";
        private readonly string _logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Assets", "Logos", "cust_logo.png");

        public SubmitFypFormController()
        {
            EnsureTableExists();
        }

        private void EnsureTableExists()
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS fyp_forms (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    SubmissionDate DATETIME NOT NULL,
                    Status VARCHAR(100) NOT NULL,
                    UserName VARCHAR(255) NOT NULL,
                    UserEmail VARCHAR(255) NOT NULL,
                    Data JSON NOT NULL,
                    PdfPath VARCHAR(500),
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                  );";

            using var cmd = new MySqlCommand(createTableQuery, connection);
            cmd.ExecuteNonQuery();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] JsonElement rawForm)
        {
            if (rawForm.ValueKind != JsonValueKind.Object)
                return BadRequest(new { message = "Invalid form data" });

            string jsonString = JsonSerializer.Serialize(rawForm);
            var form = JsonSerializer.Deserialize<FypFormRequest>(jsonString);

            if (form == null)
                return BadRequest(new { message = "Deserialization failed" });

            var pdfBytes = GeneratePdf(form);
            string fileName = $"FYP_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedForms");
            string filePath = Path.Combine(folderPath, fileName);
            Directory.CreateDirectory(folderPath);
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string insertQuery = @"
            INSERT INTO fyp_forms (SubmissionDate, Status, UserName, UserEmail, Data, PdfPath) 
            VALUES (@SubmissionDate, @Status, @UserName, @UserEmail, @Data, @PdfPath)";

            using var cmd = new MySqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("@SubmissionDate", form.SubmissionDate);
            cmd.Parameters.AddWithValue("@Status", form.Status);
            cmd.Parameters.AddWithValue("@UserName", form.UserName); // Use the authenticated user's name
            cmd.Parameters.AddWithValue("@UserEmail", form.UserEmail); // Use the authenticated user's email
            cmd.Parameters.AddWithValue("@Data", jsonString);
            cmd.Parameters.AddWithValue("@PdfPath", filePath);
            cmd.ExecuteNonQuery();

            return Ok(new { message = "Form submitted and PDF generated", filePath });
        }

        [HttpGet("download/{id}")]
        public IActionResult DownloadPdf(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            connection.Open();

            string query = "SELECT Data FROM fyp_forms WHERE Id = @Id";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", id);

            var jsonString = cmd.ExecuteScalar() as string;
            if (string.IsNullOrEmpty(jsonString))
                return NotFound(new { message = "Form data not found." });

            var form = JsonSerializer.Deserialize<FypFormRequest>(jsonString);
            if (form == null)
                return BadRequest(new { message = "Deserialization failed." });

            var pdfBytes = GeneratePdf(form); // 🔁 Regenerate on-the-fly

            return File(pdfBytes, "application/pdf", $"FYP_{id}.pdf");
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(header =>
            {
                header.Item().Row(row =>
                {
                    row.ConstantItem(100).Height(70).Image(System.IO.File.ReadAllBytes(_logoPath), ImageScaling.FitArea);

                    row.RelativeItem().PaddingLeft(10).Column(col =>
                    {
                        col.Item().Text("Capital University of Science & Technology").FontSize(16).Bold();
                        col.Item().Text("Islamabad").FontSize(12);
                        col.Item().Text("Expressway, Kahuta Road, Zone-V, Islamabad").FontSize(9);
                        col.Item().Text("Phone: 92-51-111-555-666    Fax: 92-51-4486705").FontSize(9);
                        col.Item().Text("Email: info@cust.edu.pk    Website: http://www.cust.edu.pk").FontSize(9);
                    });
                });

                header.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                header.Item().AlignCenter().Text("Office of Research, Innovation and Commercialization Initiative")
                      .FontSize(12).Bold();
                header.Item().AlignCenter().Text("Application Form for Funding of Final Year Projects (FYPs)")
                      .FontSize(11).SemiBold();
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(footer =>
            {
                footer.Item().AlignCenter().Text("Capital University of Science & Technology (CUST) - Expressway, Kahuta Road, Zone-V, Islamabad");
                footer.Item().AlignCenter().Text("Phone: 92-51-111-555-666 | Fax: 92-51-4486705 | Email: info@cust.edu.pk | Website: https://cust.edu.pk/");
                footer.Item().PaddingTop(5).AlignCenter().Text(x =>
                {
                    x.Span("Document Generated: ").FontSize(8);
                    x.Span($"{DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).SemiBold();
                });
            });
        }

        private byte[] DecodeBase64Image(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64) || base64.Length < 50) // <-- skip dummy ones
                return null;

            try
            {
                var base64Clean = base64.Split(',').Last();
                return Convert.FromBase64String(base64Clean);
            }
            catch
            {
                return null;
            }
        }


        private byte[] GeneratePdf(FypFormRequest form)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").LineHeight(1.2f));

                    page.Header().ShowOnce().Element(ComposeHeader);
                    page.Footer().ShowOnce().Element(ComposeFooter);

                    page.Content().Column(column =>
                    {
                        // Add this at the beginning of the content
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.ConstantItem(160).Text("Submitted By:").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                            row.RelativeItem().PaddingLeft(10).Text($"{form.UserName} ({form.UserEmail})").FontSize(10);
                        });
                        column.Spacing(8);

                        void SectionTitle(string title) => column.Item()
                            .PaddingTop(12)
                            .BorderBottom(1.5f).BorderColor(Colors.Blue.Darken3)
                            .PaddingBottom(5)
                            .Text(title)
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        void Field(string label, string value) => column.Item()
                            .PaddingBottom(6)
                            .Row(row =>
                            {
                                row.ConstantItem(160).Text(label).FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                                row.RelativeItem().PaddingLeft(10).Text(value ?? "-").FontSize(10).FontColor(Colors.Grey.Darken3);
                            });

                        SectionTitle("0. Submission Metadata");
                        Field("Cohort ID:", form.CohortId.ToString());
                        Field("Submission Date:", form.SubmissionDate.ToString("dd MMM yyyy HH:mm"));

                        SectionTitle("1. Supervisor Information");
                        Field("Full Name:", form.SupervisorName);
                        Field("Designation:", form.SupervisorDesignation);
                        Field("Email:", form.SupervisorEmail);
                        Field("Office Contact:", form.SupervisorOffice);
                        Field("Mobile:", form.SupervisorCell);

                        column.Item().PaddingTop(5).Text("Supervisor Signature:").FontSize(10).SemiBold();
                        column.Item().Text(form.SupervisorName?.Split(' ').FirstOrDefault() ?? "[No Signature]").FontSize(10).Italic();

                        SectionTitle("2. Student Information");

                        if (form.Students != null && form.Students.Any())
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Name
                                    columns.RelativeColumn(2); // Reg #
                                    columns.RelativeColumn(2); // Contact
                                    columns.RelativeColumn(2); // CGPA
                                });

                                // Header styling
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Name").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Reg #").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Contact").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("CGPA").FontSize(9).Bold();
                                });

                                // Row styling
                                foreach (var student in form.Students)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(student.Name).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(student.RegNo).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(student.ContactNo).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(student.Cgpa).FontSize(9);
                                }
                            });

                            column.Item().PaddingTop(10).Text("Student Signatures:").FontSize(10).SemiBold();
                            foreach (var student in form.Students)
                            {
                                column.Item().Row(row =>
                                {
                                    row.ConstantItem(160).Text(student.Name ?? "Student").FontSize(9);
                                    row.RelativeItem().Text(student.Name?.Split(' ').FirstOrDefault() ?? "[No Signature]").FontSize(9).Italic();
                                });
                            }
                        }

                        SectionTitle("3. Project Details");
                        Field("Title:", form.ProjectTitle);
                        Field("Timeline:", $"{form.StartDate} to {form.EndDate}");
                        Field("Summary:", form.ProjectSummary);
                        Field("Business Plan:", form.BusinessPlan);
                        Field("Marketing Potential:", form.MarketingPotential);
                        Field("Benefits:", form.ProjectBenefits);

                        SectionTitle("4. Deliverables");
                        if (form.Deliverables != null)
                        {
                            var deliverables = new List<string>();
                            if (form.Deliverables.HardwareSystem) deliverables.Add("✓ Hardware System");
                            if (form.Deliverables.SoftwareSystem) deliverables.Add("✓ Software System");
                            if (form.Deliverables.HwSwIntegration) deliverables.Add("✓ HW/SW Integrated System");
                            if (form.Deliverables.SimulatorDesign) deliverables.Add("✓ Simulator Design");
                            if (form.Deliverables.SoftwareSimulationResults) deliverables.Add("✓ Software Simulation Results");
                            if (form.Deliverables.TheoreticalDesign) deliverables.Add("✓ Theoretical Design");
                            if (form.Deliverables.NewTechnology) deliverables.Add("✓ New Technology / Process / Mechanism");
                            if (form.Deliverables.NewOrRecyclableMaterials) deliverables.Add("✓ New or Recyclable Materials / Products / Composites");
                            if (form.Deliverables.SustainableSoloution) deliverables.Add("✓ Sustainable Solution");
                            if (form.Deliverables.ComparativeStudy) deliverables.Add("✓ Comparative Study");
                            if (form.Deliverables.Other) deliverables.Add($"✓ Other: {form.Deliverables.OtherSpecification ?? ""}");

                            foreach (var item in deliverables)
                                column.Item().Text(text => text.Span(item).FontSize(10));
                        }

                        SectionTitle("5. Prototype Equipment");
                        if (form.PrototypeItems != null && form.PrototypeItems.Any())
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Item Name").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Units").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Unit Cost (Rs)").FontSize(9).Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Total (Rs)").FontSize(9).Bold();
                                });

                                decimal grandTotal = 0;
                                foreach (var item in form.PrototypeItems)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(item.ItemName ?? "").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(item.Units.ToString()).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(item.UnitCost.ToString("N2")).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(item.TotalCost.ToString("N2")).FontSize(9);
                                    grandTotal += item.TotalCost;
                                }

                                table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten3).Padding(5).Text("Grand Total:").FontSize(9).Bold();
                                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(grandTotal.ToString("N2")).FontSize(9).Bold();
                            });
                        }
                        else
                        {
                            column.Item().Text(text =>
                                text.Span("No prototype items added")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Medium));
                        }

                        SectionTitle("6. Students Undertaking");
                        column.Item().PaddingBottom(5).Text("We (the students involved in this FYP) hereby commit that if our FYP gets selected for funding, we will:")
                        .FontSize(10).FontColor(Colors.Grey.Darken3);

                        var points = new[]
                        {
                            "Attend the CUST Incubation Centre (CIC) for at least 5 hours/week (each student) for the required three months (27th October 2024 to 20th January 2025).",
                            "Submit a plan/schedule of our availability in the CIC.",
                            "Prepare a Standee of our project that will be displayed in the CIC.",
                            "Stay prepared for demonstration before any clients.",
                            $"Authorize Mr./Ms. {form.Undertaking?.AuthorizedPerson ?? "____________"} to get the funding cheque prepared in his/her name."
                        };

                        foreach (var point in points)
                            column.Item().Text("• " + point).FontSize(10).FontColor(Colors.Grey.Darken2);

                        SectionTitle("7. Evaluation Criteria");
                        if (form.Evaluation != null)
                        {
                            Field("Practical Possibility:", form.Evaluation.PracticalPossibility.ToString());
                            Field("Innovation:", form.Evaluation.Innovation.ToString());
                            Field("Business Plan:", form.Evaluation.BusinessPlan.ToString());
                            Field("Marketing Potential:", form.Evaluation.MarketingPotential.ToString());
                            Field("Commercialization Aptitude:", form.Evaluation.CommercializationAptitude.ToString());
                            Field("Total Score:", form.Evaluation.TotalScore.ToString());
                        }

                        SectionTitle("8. Certificate of Approval");
                        column.Item().PaddingTop(5).Text("It is further undertaken that the expenditure report of approved FYPs, along with the supporting documents and the Prototype/End Product (in case the project has developed one), or the Project Poster (if not), will be submitted to ORIC upon completion of the project.")
                        .FontSize(10).FontColor(Colors.Grey.Darken2);
                    });
                });
            });

            return document.GeneratePdf();
        }

        [HttpGet("all")]
        public IActionResult GetAllProposals()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Open();

                string query = @"
            SELECT 
                Id,
                SubmissionDate,
                Status,
                UserName,
                UserEmail,
                CreatedAt,
                PdfPath,
                Data  -- ✅ Include the Data column
            FROM fyp_forms
            ORDER BY CreatedAt DESC";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var proposals = new List<dynamic>(); // You can still use FypProposalSummary if you add a Data field

                while (reader.Read())
                {
                    proposals.Add(new
                    {
                        Id = reader.GetInt32("Id"),
                        SubmissionDate = reader.GetDateTime("SubmissionDate"),
                        Status = reader.GetString("Status"),
                        UserName = reader.GetString("UserName"),
                        UserEmail = reader.GetString("UserEmail"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        PdfPath = reader.IsDBNull(reader.GetOrdinal("PdfPath")) ? null : reader.GetString("PdfPath"),
                        Data = reader.IsDBNull(reader.GetOrdinal("Data")) ? null : reader.GetString("Data") // ✅ Include JSON string
                    });
                }

                return Ok(proposals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving proposals", error = ex.Message });
            }
        }

    }

    // Model classes remain unchanged

    // Add this class inside your SubmitFypFormController class (but outside methods)
    public class FypProposalSummary
    {
        public int Id { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string Status { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PdfPath { get; set; }
    }
    public class FypFormRequest
    {
        // Add these two properties at the top
        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("userEmail")]
        public string UserEmail { get; set; }
        [JsonPropertyName("cohortId")]
        public int CohortId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("submissionDate")]
        public DateTime SubmissionDate { get; set; }

        [JsonPropertyName("supervisorName")]
        public string SupervisorName { get; set; }

        [JsonPropertyName("supervisorDesignation")]
        public string SupervisorDesignation { get; set; }

        [JsonPropertyName("supervisorEmail")]
        public string SupervisorEmail { get; set; }

        [JsonPropertyName("supervisorOffice")]
        public string SupervisorOffice { get; set; }

        [JsonPropertyName("supervisorCell")]
        public string SupervisorCell { get; set; }

        [JsonPropertyName("supervisorSignatureBase64")]
        public string SupervisorSignatureBase64 { get; set; }

        [JsonPropertyName("projectTitle")]
        public string ProjectTitle { get; set; }

        [JsonPropertyName("projectSummary")]
        public string ProjectSummary { get; set; }

        [JsonPropertyName("businessPlan")]
        public string BusinessPlan { get; set; }

        [JsonPropertyName("marketingPotential")]
        public string MarketingPotential { get; set; }

        [JsonPropertyName("projectBenefits")]
        public string ProjectBenefits { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("students")]
        public List<Student> Students { get; set; } = new();

        [JsonPropertyName("prototypeItems")]
        public List<PrototypeItem> PrototypeItems { get; set; } = new();

        [JsonPropertyName("deliverables")]
        public Deliverables Deliverables { get; set; } = new();

        [JsonPropertyName("undertaking")]
        public Undertaking Undertaking { get; set; } = new();

        [JsonPropertyName("evaluation")]
        public Evaluation Evaluation { get; set; } = new();
    }

    public class Student
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("regNo")]
        public string RegNo { get; set; }

        [JsonPropertyName("contactNo")]
        public string ContactNo { get; set; }

        [JsonPropertyName("cgpa")]
        public string Cgpa { get; set; }

        [JsonPropertyName("signatureBase64")]
        public string SignatureBase64 { get; set; }
    }

    public class PrototypeItem
    {
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        [JsonPropertyName("units")]
        public int Units { get; set; }

        [JsonPropertyName("unitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("totalCost")]
        public decimal TotalCost { get; set; }
    }

    public class Deliverables
    {
        [JsonPropertyName("hardwareSystem")]
        public bool HardwareSystem { get; set; }

        [JsonPropertyName("softwareSystem")]
        public bool SoftwareSystem { get; set; }

        [JsonPropertyName("hwSwIntegration")]
        public bool HwSwIntegration { get; set; }

        [JsonPropertyName("simulatorDesign")]
        public bool SimulatorDesign { get; set; }

        [JsonPropertyName("softwareSimulationResults")]
        public bool SoftwareSimulationResults { get; set; }

        [JsonPropertyName("theoreticalDesign")]
        public bool TheoreticalDesign { get; set; }

        [JsonPropertyName("newTechnology")]
        public bool NewTechnology { get; set; }

        [JsonPropertyName("newOrRecyclableMaterials")]
        public bool NewOrRecyclableMaterials { get; set; }

        [JsonPropertyName("sustainableSoloution")]
        public bool SustainableSoloution { get; set; }

        [JsonPropertyName("comparativeStudy")]
        public bool ComparativeStudy { get; set; }

        [JsonPropertyName("other")]
        public bool Other { get; set; }

        [JsonPropertyName("otherSpecification")]
        public string OtherSpecification { get; set; }
    }

    public class Undertaking
    {
        public bool IncubationCentre { get; set; }
        public bool FundingDisbursement { get; set; }
        public string AuthorizedPerson { get; set; }
    }

    public class Evaluation
    {
        [JsonPropertyName("practicalPossibility")]
        public int PracticalPossibility { get; set; }

        [JsonPropertyName("innovation")]
        public int Innovation { get; set; }

        [JsonPropertyName("businessPlan")]
        public int BusinessPlan { get; set; }

        [JsonPropertyName("marketingPotential")]
        public int MarketingPotential { get; set; }

        [JsonPropertyName("commercializationAptitude")]
        public int CommercializationAptitude { get; set; }

        [JsonPropertyName("totalScore")]
        public int TotalScore { get; set; }
    }
}
