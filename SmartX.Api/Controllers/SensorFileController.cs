using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/sensor-files")]
    public class SensorFileController : ControllerBase
    {
        private readonly SensorFileService _sensorFileService;

        public SensorFileController(SensorFileService sensorFileService)
        {
            _sensorFileService = sensorFileService;
        }

        [HttpPost("{macAddress}")]
        public async Task<IActionResult> UploadFile(
            string macAddress,
            IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "A file is required."
                });
            }

            await using var stream = file.OpenReadStream();

            await _sensorFileService.UploadFileAsync(
                macAddress,
                file.FileName,
                stream);

            return Ok(new
            {
                message = "File uploaded successfully.",
                sensorMacAddress = macAddress,
                fileName = file.FileName
            });
        }

        [HttpGet("{macAddress}")]
        public async Task<IActionResult> GetFiles(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message = "MAC address is required."
                });
            }

            var files =
                await _sensorFileService.GetFilesAsync(macAddress);

            return Ok(new
            {
                sensorMacAddress = macAddress,
                files
            });
        }
    }
}