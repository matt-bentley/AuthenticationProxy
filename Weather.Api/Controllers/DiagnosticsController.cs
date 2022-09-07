using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Weather.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            StringBuilder sb = new();
            foreach (var header in Request.Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value.FirstOrDefault()}");
            }

            return Ok(sb.ToString());
        }
    }
}