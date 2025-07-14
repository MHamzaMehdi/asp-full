using Microsoft.AspNetCore.Mvc;

namespace Tetsing_app_1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestControl : ControllerBase
    {
        // GET: api/TestControl/ping
        [HttpGet("ping")]
        public ContentResult Ping()
        {
            return Content("API is working successfully!", "text/plain");
        }
    }
}
