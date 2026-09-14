using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/sensor-files")]
    public class SensorFileController : ControllerBase
    {
        private readonly SensorFileService _sensorFileService;
        private readonly ILogger<SensorFileController> _logger;

        public SensorFileController(
            SensorFileService sensorFileService,
            ILogger<SensorFileController> logger)
        {
            _sensorFileService =
                sensorFileService;

            _logger =
                logger;
        }

        [HttpPost("{macAddress}")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile(
            string macAddress,
            IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message =
                        "MAC address is required."
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message =
                        "A file is required."
                });
            }

            if (!_sensorFileService.IsFileSizeAllowed(
                    file.Length))
            {
                return BadRequest(new
                {
                    message =
                        "The file size cannot exceed 10 MB."
                });
            }

            if (!_sensorFileService.IsAllowedFileType(
                    file.FileName,
                    file.ContentType))
            {
                return BadRequest(new
                {
                    message =
                        "Unsupported file type. Allowed types are PDF, TXT, CSV, DOC, DOCX, XLS, XLSX, PPT and PPTX."
                });
            }

            try
            {
                await using var stream =
                    file.OpenReadStream();

                await _sensorFileService.UploadFileAsync(
                    macAddress,
                    file.FileName,
                    file.ContentType,
                    stream);

                _logger.LogInformation(
                    "File '{FileName}' uploaded for sensor '{MacAddress}'.",
                    file.FileName,
                    macAddress);

                return Ok(new
                {
                    message =
                        "File uploaded successfully.",

                    sensorMacAddress =
                        macAddress,

                    fileName =
                        file.FileName,

                    contentType =
                        file.ContentType,

                    size =
                        file.Length
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid file upload attempt for sensor '{MacAddress}'.",
                    macAddress);

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error uploading file for sensor '{MacAddress}'.",
                    macAddress);

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while uploading the file."
                    });
            }
        }

        [HttpGet("{macAddress}")]
        public async Task<IActionResult> GetFiles(
            string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message =
                        "MAC address is required."
                });
            }

            try
            {
                var files =
                    await _sensorFileService.GetFilesAsync(
                        macAddress);

                return Ok(new
                {
                    sensorMacAddress =
                        macAddress,

                    files
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving files for sensor '{MacAddress}'.",
                    macAddress);

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while retrieving the files."
                    });
            }
        }

        [HttpGet("{macAddress}/download/{fileName}")]
        public async Task<IActionResult> DownloadFile(
            string macAddress,
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                return BadRequest(new
                {
                    message =
                        "MAC address is required."
                });
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new
                {
                    message =
                        "File name is required."
                });
            }

            try
            {
                var file =
                    await _sensorFileService.DownloadFileAsync(
                        macAddress,
                        fileName);

                if (file == null)
                {
                    return NotFound(new
                    {
                        message =
                            "The requested file was not found."
                    });
                }

                _logger.LogInformation(
                    "File '{FileName}' downloaded for sensor '{MacAddress}'.",
                    fileName,
                    macAddress);

                return File(
                    file.Content,
                    file.ContentType,
                    file.FileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error downloading file '{FileName}' for sensor '{MacAddress}'.",
                    fileName,
                    macAddress);

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while downloading the file."
                    });
            }
        }
    }
}