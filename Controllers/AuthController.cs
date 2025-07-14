using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Net;
using System.Net.Mail;
using System.Web;
using Tetsing_app_1.Models;
using BCrypt.Net;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";

        // ✅ Forgot Password: Generate Token and Email Link
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required.");

            string resetToken = Guid.NewGuid().ToString();

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Check if user exists
                var checkQuery = "SELECT COUNT(*) FROM User WHERE Email = @Email";
                using (var cmd = new MySqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    if (!exists)
                        return NotFound("Email not registered.");
                }

                // 2. Store token
                var tokenQuery = "UPDATE User SET ResetToken = @Token WHERE Email = @Email";
                using (var cmd = new MySqlCommand(tokenQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Token", resetToken);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.ExecuteNonQuery();
                }
            }

            // ✅ Correct frontend port
            var resetLink = $"http://localhost:8080/reset-password?email={request.Email}&token={HttpUtility.UrlEncode(resetToken)}";
            SendEmail(request.Email, "Reset your password", $"Click here to reset your password: <a href='{resetLink}'>Reset Password</a>");

            return Ok(new { message = "Password reset email sent." });
        }

        // ✅ Reset Password: Validate token and update password
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest("Missing required fields.");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // 1. Validate token
                var checkQuery = "SELECT COUNT(*) FROM User WHERE Email = @Email AND ResetToken = @Token";
                using (var cmd = new MySqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.Parameters.AddWithValue("@Token", request.Token);
                    var isValid = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    if (!isValid)
                        return BadRequest("Invalid or expired token.");
                }

                // 2. Hash new password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // 3. Update password and clear token
                var updateQuery = "UPDATE User SET Password = @NewPassword, ResetToken = NULL WHERE Email = @Email";
                using (var cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@NewPassword", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", request.Email);
                    cmd.ExecuteNonQuery();
                }
            }

            return Ok(new { message = "Password reset successfully." });
        }

        // ✅ Email sender
        private void SendEmail(string to, string subject, string html)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password"),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress("your-email@gmail.com", "ORIC Portal"),
                Subject = subject,
                Body = html,
                IsBodyHtml = true
            };

            mail.To.Add(to);
            smtpClient.Send(mail);
        }
    }
}
