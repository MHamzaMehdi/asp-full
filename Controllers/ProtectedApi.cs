using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ✅ Protects this whole controller
    public class ProtectedApi : ControllerBase
    {
        [HttpGet("user-info")]
        public IActionResult GetUserInfo()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "This is a protected route.",
                user = new
                {
                    email,
                    role
                }
            });
        }

        [Authorize(Roles = "admin")]
        [HttpGet("admin-only")]
        public IActionResult GetAdminOnlyData()
        {
            return Ok(new
            {
                message = "Only admins can see this!",
                success = true
            });
        }
    }
}
