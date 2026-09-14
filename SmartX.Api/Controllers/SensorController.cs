using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorController : ControllerBase
    {
        private readonly SensorTableService _sensorTableService;

        public SensorController(SensorTableService sensorTableService)
        {
            _sensorTableService = sensorTableService;
        }

        // GET: api/Sensor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sensor>>> GetSensors()
        {
            var sensors = await _sensorTableService.GetSensorsAsync();

            return Ok(sensors);
        }

        // GET: api/Sensor/{macAddress}
        [HttpGet("{macAddress}")]
        public async Task<ActionResult<Sensor>> GetSensor(
            string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            var sensor = await _sensorTableService.GetSensorAsync(
                macAddress);

            if (sensor == null)
            {
                return NotFound(new
                {
                    message = "Sensor not found."
                });
            }

            return Ok(sensor);
        }

        // POST: api/Sensor
        [HttpPost]
        public async Task<ActionResult<Sensor>> CreateSensor(
            [FromBody] Sensor sensor)
        {
            if (string.IsNullOrWhiteSpace(sensor.MacAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(sensor.Location))
            {
                return BadRequest(new
                {
                    message = "Location is required."
                });
            }

            if (string.IsNullOrWhiteSpace(sensor.Category))
            {
                return BadRequest(new
                {
                    message = "Category is required."
                });
            }

            var existingSensor =
                await _sensorTableService.GetSensorAsync(
                    sensor.MacAddress);

            if (existingSensor != null)
            {
                return Conflict(new
                {
                    message = "A sensor with this MAC address already exists."
                });
            }

            await _sensorTableService.CreateSensorAsync(sensor);

            return CreatedAtAction(
                nameof(GetSensor),
                new
                {
                    macAddress = sensor.MacAddress
                },
                sensor);
        }

        // PUT: api/Sensor/{macAddress}
        [HttpPut("{macAddress}")]
        public async Task<ActionResult<Sensor>> UpdateSensor(
            string macAddress,
            [FromBody] Sensor updatedSensor)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(updatedSensor.Location))
            {
                return BadRequest(new
                {
                    message = "Location is required."
                });
            }

            if (string.IsNullOrWhiteSpace(updatedSensor.Category))
            {
                return BadRequest(new
                {
                    message = "Category is required."
                });
            }

            var existingSensor =
                await _sensorTableService.GetSensorAsync(macAddress);

            if (existingSensor == null)
            {
                return NotFound(new
                {
                    message = "Sensor not found."
                });
            }

            var updated = await _sensorTableService.UpdateSensorAsync(
                macAddress,
                updatedSensor);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Sensor not found."
                });
            }

            var sensor = await _sensorTableService.GetSensorAsync(
                macAddress);

            return Ok(sensor);
        }

        // DELETE: api/Sensor/{macAddress}
        [HttpDelete("{macAddress}")]
        public async Task<ActionResult> DeleteSensor(
            string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            var deleted = await _sensorTableService.DeleteSensorAsync(
                macAddress);

            if (!deleted)
            {
                return NotFound(new
                {
                    message = "Sensor not found."
                });
            }

            return Ok(new
            {
                message = "Sensor deleted successfully."
            });
        }
    }
}