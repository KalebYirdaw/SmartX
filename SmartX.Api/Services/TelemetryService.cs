using Azure;
using Azure.Data.Tables;
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class TelemetryService
    {
        private readonly TableClient _tableClient;

        private const string TableName = "Telemetry";

        public TelemetryService(IConfiguration configuration)
        {
            var connectionString =
                configuration.GetConnectionString("AzureStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "AzureStorage connection string is missing.");
            }

            _tableClient = new TableClient(
                connectionString,
                TableName);
        }

        // Creates the Telemetry table if it does not already exist.
        public async Task InitializeAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();
        }

        // Adds a telemetry reading to Azure Table Storage.
        public async Task AddTelemetryAsync(Telemetry telemetry)
        {
            var timestamp = telemetry.Timestamp.ToUniversalTime();

            var rowKey =
                $"{timestamp:yyyyMMddHHmmssfff}-{Guid.NewGuid()}";

            var entity = new TableEntity(
                telemetry.SensorMacAddress,
                rowKey)
            {
                ["Timestamp"] = timestamp,
                ["Metric"] = telemetry.Metric,
                ["Value"] = telemetry.Value
            };

            await _tableClient.AddEntityAsync(entity);
        }

        // Gets all telemetry readings for a sensor.
        public async Task<List<Telemetry>> GetTelemetryAsync(
            string sensorMacAddress)
        {
            var telemetryList = new List<Telemetry>();

            await foreach (var entity in
                _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{sensorMacAddress}'"))
            {
                telemetryList.Add(new Telemetry
                {
                    SensorMacAddress = entity.PartitionKey,
                    Timestamp = entity.GetDateTime("Timestamp") ?? DateTime.MinValue,
                    Metric = entity.GetString("Metric") ?? string.Empty,
                    Value = entity.GetDouble("Value") ?? 0
                });
            }

            return telemetryList
                .OrderByDescending(t => t.Timestamp)
                .ToList();
        }
    }
}