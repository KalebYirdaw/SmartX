using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace SmartX.Api.Services
{
    public class SensorFileService
    {
        private readonly ShareClient _shareClient;
        private readonly ILogger<SensorFileService> _logger;

        // Required Azure File Share name from the PoE rubric.
        private const string ShareName = "staff-docs";

        private const string DirectoryName = "sensor-files";

        // Maximum file size: 10 MB.
        private const long MaxFileSize = 10 * 1024 * 1024;

        // Allowed document/file types.
        private static readonly Dictionary<string, string> AllowedFileTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = "application/pdf",
                [".txt"] = "text/plain",
                [".csv"] = "text/csv",
                [".doc"] = "application/msword",
                [".docx"] =
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".xls"] = "application/vnd.ms-excel",
                [".xlsx"] =
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".ppt"] = "application/vnd.ms-powerpoint",
                [".pptx"] =
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

        public SensorFileService(
            IConfiguration configuration,
            ILogger<SensorFileService> logger)
        {
            _logger = logger;

            var connectionString =
                configuration.GetConnectionString(
                    "AzureFileStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AzureFileStorage connection string is missing.");
            }

            _shareClient = new ShareClient(
                connectionString,
                ShareName);
        }

        // ============================================================
        // AZURE FILE SHARE INITIALIZATION
        // ============================================================

        public async Task InitializeAsync()
        {
            await _shareClient.CreateIfNotExistsAsync();

            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            await directoryClient.CreateIfNotExistsAsync();

            _logger.LogInformation(
                "Azure File Share '{ShareName}' initialized with directory '{DirectoryName}'.",
                ShareName,
                DirectoryName);
        }

        // ============================================================
        // FILE VALIDATION
        // ============================================================

        public bool IsAllowedFileType(
            string fileName,
            string contentType)
        {
            var extension =
                Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            if (!AllowedFileTypes.TryGetValue(
                    extension,
                    out var expectedContentType))
            {
                return false;
            }

            // Accept an empty MIME type, otherwise require
            // the uploaded MIME type to match the extension.
            return string.IsNullOrWhiteSpace(contentType)
                   ||
                   contentType.Equals(
                       expectedContentType,
                       StringComparison.OrdinalIgnoreCase);
        }

        public bool IsFileSizeAllowed(long fileSize)
        {
            return fileSize > 0 &&
                   fileSize <= MaxFileSize;
        }

        // ============================================================
        // RUBRIC-COMPATIBLE DOCUMENT API
        // ============================================================

        // POST /api/documents/upload
        public async Task<DocumentMetadata> UploadDocumentAsync(
            string fileName,
            string contentType,
            Stream fileStream)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.");
            }

            if (fileStream == null)
            {
                throw new ArgumentException(
                    "File stream is required.");
            }

            if (!IsAllowedFileType(
                    fileName,
                    contentType))
            {
                throw new InvalidOperationException(
                    "The selected file type is not supported.");
            }

            if (!fileStream.CanRead)
            {
                throw new InvalidOperationException(
                    "The uploaded file cannot be read.");
            }

            if (fileStream.Length <= 0)
            {
                throw new InvalidOperationException(
                    "The uploaded file is empty.");
            }

            if (!IsFileSizeAllowed(
                    fileStream.Length))
            {
                throw new InvalidOperationException(
                    "The file size cannot exceed 10 MB.");
            }

            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var safeFileName =
                SanitizeFileName(fileName);

            var fileClient =
                directoryClient.GetFileClient(
                    safeFileName);

            // Create the Azure File with the correct size.
            await fileClient.CreateAsync(
                fileStream.Length);

            // Ensure the stream starts from the beginning.
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            // Stream the uploaded content to Azure File Share.
            await fileClient.UploadAsync(
                fileStream);

            // Retrieve Azure File properties.
            var properties =
                await fileClient.GetPropertiesAsync();

            var metadata =
                new DocumentMetadata
                {
                    FileName =
                        safeFileName,

                    ContentType =
                        contentType,

                    Size =
                        properties.Value.ContentLength,

                    UploadDate =
                        properties.Value.LastModified
                };

            _logger.LogInformation(
                "Document '{FileName}' uploaded to Azure File Share '{ShareName}'. Size: {FileSize} bytes.",
                safeFileName,
                ShareName,
                metadata.Size);

            return metadata;
        }

        // GET /api/documents
        public async Task<List<DocumentMetadata>>
            GetDocumentsAsync()
        {
            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var documents =
                new List<DocumentMetadata>();

            await foreach (var item in
                directoryClient.GetFilesAndDirectoriesAsync())
            {
                // Ignore directories.
                if (!item.FileSize.HasValue)
                {
                    continue;
                }

                var fileClient =
                    directoryClient.GetFileClient(
                        item.Name);

                // Get the actual Azure File properties.
                var properties =
                    await fileClient.GetPropertiesAsync();

                var extension =
                    Path.GetExtension(
                        item.Name);

                documents.Add(
                    new DocumentMetadata
                    {
                        FileName =
                            item.Name,

                        ContentType =
                            GetContentType(
                                extension),

                        Size =
                            properties.Value.ContentLength,

                        UploadDate =
                            properties.Value.LastModified
                    });
            }

            return documents
                .OrderByDescending(
                    document => document.UploadDate)
                .ToList();
        }

        // GET /api/documents/download/{fileName}
        public async Task<DocumentDownload?>
            DownloadDocumentAsync(
                string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.");
            }

            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var safeFileName =
                SanitizeFileName(fileName);

            var fileClient =
                directoryClient.GetFileClient(
                    safeFileName);

            if (!await fileClient.ExistsAsync())
            {
                return null;
            }

            // Stream the file from Azure File Share.
            var download =
                await fileClient.DownloadAsync();

            var extension =
                Path.GetExtension(
                    safeFileName);

            return new DocumentDownload
            {
                FileName =
                    safeFileName,

                ContentType =
                    GetContentType(
                        extension),

                Content =
                    download.Value.Content
            };
        }

        // ============================================================
        // EXISTING SENSOR FILE FUNCTIONALITY
        // ============================================================

        public async Task UploadFileAsync(
            string sensorMacAddress,
            string fileName,
            string contentType,
            Stream fileStream)
        {
            if (string.IsNullOrWhiteSpace(
                    sensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                throw new ArgumentException(
                    "File name is required.");
            }

            if (fileStream == null)
            {
                throw new ArgumentException(
                    "File stream is required.");
            }

            if (!IsAllowedFileType(
                    fileName,
                    contentType))
            {
                throw new InvalidOperationException(
                    "The selected file type is not supported.");
            }

            if (!fileStream.CanRead)
            {
                throw new InvalidOperationException(
                    "The uploaded file cannot be read.");
            }

            if (fileStream.Length <= 0)
            {
                throw new InvalidOperationException(
                    "The uploaded file is empty.");
            }

            if (!IsFileSizeAllowed(
                    fileStream.Length))
            {
                throw new InvalidOperationException(
                    "The file size cannot exceed 10 MB.");
            }

            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var safeMacAddress =
                SanitizeMacAddress(
                    sensorMacAddress);

            var safeFileName =
                SanitizeFileName(
                    fileName);

            var storedFileName =
                $"{safeMacAddress}_{safeFileName}";

            var fileClient =
                directoryClient.GetFileClient(
                    storedFileName);

            await fileClient.CreateAsync(
                fileStream.Length);

            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            await fileClient.UploadAsync(
                fileStream);

            _logger.LogInformation(
                "Uploaded file '{FileName}' for sensor '{SensorMacAddress}'. Size: {FileSize} bytes.",
                safeFileName,
                sensorMacAddress,
                fileStream.Length);
        }

        public async Task<List<SensorFileMetadata>>
            GetFilesAsync(
                string sensorMacAddress)
        {
            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var safeMacAddress =
                SanitizeMacAddress(
                    sensorMacAddress);

            var prefix =
                $"{safeMacAddress}_";

            var files =
                new List<SensorFileMetadata>();

            await foreach (var item in
                directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (!item.FileSize.HasValue)
                {
                    continue;
                }

                if (!item.Name.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var originalFileName =
                    item.Name.Substring(
                        prefix.Length);

                var extension =
                    Path.GetExtension(
                        originalFileName);

                var contentType =
                    GetContentType(
                        extension);

                files.Add(
                    new SensorFileMetadata
                    {
                        FileName =
                            originalFileName,

                        StoredFileName =
                            item.Name,

                        ContentType =
                            contentType,

                        Size =
                            item.FileSize.Value
                    });
            }

            return files
                .OrderBy(
                    f => f.FileName)
                .ToList();
        }

        public async Task<SensorFileDownload?>
            DownloadFileAsync(
                string sensorMacAddress,
                string fileName)
        {
            if (string.IsNullOrWhiteSpace(
                    sensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                throw new ArgumentException(
                    "File name is required.");
            }

            var directoryClient =
                _shareClient.GetDirectoryClient(
                    DirectoryName);

            var safeMacAddress =
                SanitizeMacAddress(
                    sensorMacAddress);

            var safeFileName =
                SanitizeFileName(
                    fileName);

            var storedFileName =
                $"{safeMacAddress}_{safeFileName}";

            var fileClient =
                directoryClient.GetFileClient(
                    storedFileName);

            if (!await fileClient.ExistsAsync())
            {
                return null;
            }

            var download =
                await fileClient.DownloadAsync();

            var extension =
                Path.GetExtension(
                    safeFileName);

            return new SensorFileDownload
            {
                FileName =
                    safeFileName,

                ContentType =
                    GetContentType(
                        extension),

                Content =
                    download.Value.Content
            };
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private static string SanitizeMacAddress(
            string sensorMacAddress)
        {
            return new string(
                sensorMacAddress
                    .Where(char.IsLetterOrDigit)
                    .ToArray());
        }

        private static string SanitizeFileName(
            string fileName)
        {
            var name =
                Path.GetFileName(
                    fileName);

            foreach (var invalidCharacter in
                     Path.GetInvalidFileNameChars())
            {
                name =
                    name.Replace(
                        invalidCharacter.ToString(),
                        "_");
            }

            return name;
        }

        private static string GetContentType(
            string extension)
        {
            if (AllowedFileTypes.TryGetValue(
                    extension,
                    out var contentType))
            {
                return contentType;
            }

            return "application/octet-stream";
        }
    }

    // ================================================================
    // DOCUMENT MODELS
    // ================================================================

    public class DocumentMetadata
    {
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }

        public DateTimeOffset UploadDate { get; set; }
    }

    public class DocumentDownload
    {
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public Stream Content { get; set; } = Stream.Null;
    }

    // ================================================================
    // EXISTING SENSOR FILE MODELS
    // ================================================================

    public class SensorFileMetadata
    {
        public string FileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long Size { get; set; }
    }

    public class SensorFileDownload
    {
        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public Stream Content { get; set; } = Stream.Null;
    }
}