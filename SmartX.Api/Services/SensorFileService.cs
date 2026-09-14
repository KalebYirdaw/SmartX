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

            // Accept the browser-provided MIME type when it
            // matches the expected type.
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

        public async Task UploadFileAsync(
            string sensorMacAddress,
            string fileName,
            string contentType,
            Stream fileStream)
        {
            if (string.IsNullOrWhiteSpace(sensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

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

            if (!IsFileSizeAllowed(fileStream.Length))
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
                SanitizeFileName(fileName);

            var storedFileName =
                $"{safeMacAddress}_{safeFileName}";

            var fileClient =
                directoryClient.GetFileClient(
                    storedFileName);

            await fileClient.CreateAsync(
                fileStream.Length);

            await fileClient.UploadAsync(
                fileStream);

            _logger.LogInformation(
                "Uploaded file '{FileName}' for sensor '{SensorMacAddress}'. Size: {FileSize} bytes.",
                safeFileName,
                sensorMacAddress,
                fileStream.Length);
        }

        public async Task<List<SensorFileMetadata>> GetFilesAsync(
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
                    GetContentType(extension);

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
                .OrderBy(f => f.FileName)
                .ToList();
        }

        public async Task<SensorFileDownload?> DownloadFileAsync(
            string sensorMacAddress,
            string fileName)
        {
            if (string.IsNullOrWhiteSpace(
                    sensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
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
                    GetContentType(extension),

                Content =
                    download.Value.Content
            };
        }

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