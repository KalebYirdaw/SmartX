using Azure;
using Azure.Data.Tables;
using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    public class SensorTableService
    {
        private readonly TableClient _tableClient;

        private const string TableName = "Sensors";

        public SensorTableService(IConfiguration configuration)
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

        // Creates the Sensors table if it does not already exist.
        public async Task InitializeAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();
        }

        // Gets all sensors from Azure Table Storage.
        public async Task<List<Sensor>> GetSensorsAsync()
        {
            var sensors = new List<Sensor>();

            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                sensors.Add(new Sensor
                {
                    MacAddress = entity.RowKey,
                    Location = entity.GetString("Location") ?? string.Empty,
                    Category = entity.GetString("Category") ?? string.Empty
                });
            }

            return sensors;
        }

        // Gets one sensor using its MAC address.
        public async Task<Sensor?> GetSensorAsync(string macAddress)
        {
            try
            {
                var entity = await _tableClient.GetEntityAsync<TableEntity>(
                    "Sensor",
                    macAddress);

                return new Sensor
                {
                    MacAddress = entity.Value.RowKey,
                    Location = entity.Value.GetString("Location") ?? string.Empty,
                    Category = entity.Value.GetString("Category") ?? string.Empty
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        // Creates a new sensor in Azure Table Storage.
        public async Task CreateSensorAsync(Sensor sensor)
        {
            var entity = new TableEntity(
                "Sensor",
                sensor.MacAddress)
            {
                ["Location"] = sensor.Location,
                ["Category"] = sensor.Category
            };

            await _tableClient.AddEntityAsync(entity);
        }

        // Updates an existing sensor.
        public async Task<bool> UpdateSensorAsync(
            string macAddress,
            Sensor sensor)
        {
            try
            {
                var entity = new TableEntity(
                    "Sensor",
                    macAddress)
                {
                    ["Location"] = sensor.Location,
                    ["Category"] = sensor.Category
                };

                await _tableClient.UpdateEntityAsync(
                    entity,
                    ETag.All,
                    TableUpdateMode.Replace);

                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        // Deletes an existing sensor.
        public async Task<bool> DeleteSensorAsync(string macAddress)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(
                    "Sensor",
                    macAddress);

                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}