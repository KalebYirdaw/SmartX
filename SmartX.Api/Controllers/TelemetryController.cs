using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryController : ControllerBase
    {
        private readonly TelemetryService _telemetryService;

        public TelemetryController(TelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        // POST: api/Telemetry
        [HttpPost]
        public async Task<ActionResult> AddTelemetry(
            [FromBody] Telemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(telemetry.SensorMacAddress))
            {
                return BadRequest(new
                {
                    message = "Sensor MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(telemetry.Metric))
            {
                return BadRequest(new
                {
                    message = "Metric is required."
                });
            }

            if (telemetry.Timestamp == default)
            {
                telemetry.Timestamp = DateTime.UtcNow;
            }

            await _telemetryService.AddTelemetryAsync(telemetry);

            return Ok(new
            {
                message = "Telemetry added successfully.",
                telemetry
            });
        }

        // GET: api/Telemetry/{sensorMacAddress}
        [HttpGet("{sensorMacAddress}")]
        public async Task<ActionResult<IEnumerable<Telemetry>>> GetTelemetry(
            string sensorMacAddress)
        {
            if (string.IsNullOrWhiteSpace(sensorMacAddress))
            {
                return BadRequest(new
                {
                    message = "Sensor MAC address is required."
                });
            }

            var telemetry =
                await _telemetryService.GetTelemetryAsync(
                    sensorMacAddress);

            return Ok(telemetry);
        }
    }
}