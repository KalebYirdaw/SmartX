using Azure.Storage.Files.Shares;

namespace SmartX.Api.Services
{
    public class SensorFileService
    {
        private readonly ShareClient _shareClient;

        private const string ShareName = "sensorfiles";
        private const string DirectoryName = "sensor-files";

        public SensorFileService(IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("AzureFileStorage");

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
                _shareClient.GetDirectoryClient(DirectoryName);

            await directoryClient.CreateIfNotExistsAsync();
        }

        public async Task UploadFileAsync(
            string sensorMacAddress,
            string fileName,
            Stream fileStream)
        {
            var directoryClient =
                _shareClient.GetDirectoryClient(DirectoryName);

            var safeMacAddress = new string(
                sensorMacAddress
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

            var safeFileName = SanitizeFileName(fileName);

            var storedFileName =
                $"{safeMacAddress}_{safeFileName}";

            var fileClient =
                directoryClient.GetFileClient(storedFileName);

            await fileClient.CreateAsync(fileStream.Length);

            await fileClient.UploadAsync(fileStream);
        }

        public async Task<List<string>> GetFilesAsync(
            string sensorMacAddress)
        {
            var directoryClient =
                _shareClient.GetDirectoryClient(DirectoryName);

            var safeMacAddress = new string(
                sensorMacAddress
                    .Where(char.IsLetterOrDigit)
                    .ToArray());

            var prefix = $"{safeMacAddress}_";

            var files = new List<string>();

            await foreach (var item in
                directoryClient.GetFilesAndDirectoriesAsync())
            {
                if (item.FileSize.HasValue &&
                    item.Name.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(
                        item.Name.Substring(prefix.Length));
                }
            }

            return files;
        }

        private static string SanitizeFileName(string fileName)
        {
            var name = Path.GetFileName(fileName);

            foreach (var invalidCharacter in
                     Path.GetInvalidFileNameChars())
            {
                name = name.Replace(
                    invalidCharacter.ToString(),
                    "_");
            }

            return name;
        }
    }
}