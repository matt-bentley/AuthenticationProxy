using Microsoft.AspNetCore.Mvc;

namespace Weather.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private static readonly string _secret = Guid.NewGuid().ToString();

        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        [HttpGet("secret")]
        public IActionResult GetSecret()
        {
            return Ok(_secret);
        }
    }
}