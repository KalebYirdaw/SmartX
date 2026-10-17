using Microsoft.AspNetCore.Mvc;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "Healthy",
        service = "SmartX.Api",
        timestampUtc = DateTime.UtcNow
    });
}
