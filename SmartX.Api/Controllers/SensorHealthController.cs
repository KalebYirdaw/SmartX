using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorHealthController : ControllerBase
    {
        private readonly SensorHealthService _sensorHealthService;

        public SensorHealthController(
            SensorHealthService sensorHealthService)
        {
            _sensorHealthService = sensorHealthService;
        }

        // GET: api/SensorHealth/{sensorMacAddress}
        [HttpGet("{sensorMacAddress}")]
        public async Task<ActionResult<SensorHealth>> GetSensorHealth(
            string sensorMacAddress)
        {
            if (string.IsNullOrWhiteSpace(sensorMacAddress))
            {
                return BadRequest(new
                {
                    message = "Sensor MAC address is required."
                });
            }

            try
            {
                var health =
                    await _sensorHealthService.GetSensorHealthAsync(
                        sensorMacAddress);

                return Ok(health);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}