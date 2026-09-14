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
        private readonly MockTelemetryGenerator _mockTelemetryGenerator;

        public TelemetryController(
            TelemetryService telemetryService,
            MockTelemetryGenerator mockTelemetryGenerator)
        {
            _telemetryService = telemetryService;
            _mockTelemetryGenerator = mockTelemetryGenerator;
        }

        // POST: api/Telemetry
        [HttpPost]
        public async Task<ActionResult> AddTelemetry(
            [FromBody] Telemetry telemetry)
        {
            if (string.IsNullOrWhiteSpace(
                    telemetry.SensorMacAddress))
            {
                return BadRequest(new
                {
                    message = "Sensor MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    telemetry.Metric))
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

            await _telemetryService.AddTelemetryAsync(
                telemetry);

            return Ok(new
            {
                message = "Telemetry added successfully.",
                telemetry
            });
        }

        // GET: api/Telemetry/{sensorMacAddress}
        [HttpGet("{sensorMacAddress}")]
        public async Task<ActionResult<IEnumerable<Telemetry>>>
            GetTelemetry(string sensorMacAddress)
        {
            if (string.IsNullOrWhiteSpace(
                    sensorMacAddress))
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

        // POST:
        // api/Telemetry/seed/{sensorMacAddress}?count=100
        [HttpPost("seed/{sensorMacAddress}")]
        public async Task<ActionResult> SeedTelemetry(
            string sensorMacAddress,
            [FromQuery] int count = 100)
        {
            if (string.IsNullOrWhiteSpace(
                    sensorMacAddress))
            {
                return BadRequest(new
                {
                    message = "Sensor MAC address is required."
                });
            }

            if (count < 1 || count > 5000)
            {
                return BadRequest(new
                {
                    message =
                        "Count must be between 1 and 5000."
                });
            }

            var generatedTelemetry =
                _mockTelemetryGenerator.Generate(
                    sensorMacAddress,
                    count);

            foreach (var telemetry in generatedTelemetry)
            {
                await _telemetryService.AddTelemetryAsync(
                    telemetry);
            }

            return Ok(new
            {
                message =
                    "Mock telemetry generated successfully.",

                sensorMacAddress,

                readingsGenerated =
                    generatedTelemetry.Count
            });
        }
    }
}