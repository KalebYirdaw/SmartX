using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly SensorFileService _fileService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            SensorFileService fileService,
            ILogger<DocumentsController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        // POST /api/documents/upload
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadDocument(
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "A file is required."
                });
            }

            if (!_fileService.IsFileSizeAllowed(file.Length))
            {
                return BadRequest(new
                {
                    message = "The file size cannot exceed 10 MB."
                });
            }

            if (!_fileService.IsAllowedFileType(
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

                var metadata =
                    await _fileService.UploadDocumentAsync(
                        file.FileName,
                        file.ContentType,
                        stream);

                _logger.LogInformation(
                    "Document '{FileName}' uploaded successfully.",
                    file.FileName);

                return Ok(new
                {
                    message = "Document uploaded successfully.",
                    document = metadata
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid document upload attempt for '{FileName}'.",
                    file.FileName);

                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error uploading document '{FileName}'.",
                    file.FileName);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "An error occurred while uploading the document."
                    });
            }
        }

        // GET /api/documents
        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                var documents =
                    await _fileService.GetDocumentsAsync();

                return Ok(new
                {
                    count = documents.Count,
                    documents
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving documents.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "An error occurred while retrieving documents."
                    });
            }
        }

        // GET /api/documents/download/{fileName}
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadDocument(
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new
                {
                    message = "File name is required."
                });
            }

            try
            {
                var document =
                    await _fileService.DownloadDocumentAsync(
                        fileName);

                if (document == null)
                {
                    return NotFound(new
                    {
                        message = "The requested document was not found."
                    });
                }

                _logger.LogInformation(
                    "Document '{FileName}' downloaded successfully.",
                    fileName);

                return File(
                    document.Content,
                    document.ContentType,
                    document.FileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error downloading document '{FileName}'.",
                    fileName);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message =
                            "An error occurred while downloading the document."
                    });
            }
        }
    }
}