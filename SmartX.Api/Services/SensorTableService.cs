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

        // ============================================================
        // GET ALL SENSORS / FILTER BY CATEGORY
        // ============================================================

        public async Task<List<Sensor>> GetSensorsAsync(
            string? category = null)
        {
            var sensors = new List<Sensor>();

            // No category supplied:
            // return all sensors.
            if (string.IsNullOrWhiteSpace(category))
            {
                await foreach (
                    var entity in
                    _tableClient.QueryAsync<TableEntity>())
                {
                    sensors.Add(
                        MapSensor(entity));
                }

                return sensors;
            }

            // Category supplied:
            // filter directly in Azure Table Storage.
            var filter =
                TableClient.CreateQueryFilter(
                    $"Category eq {category}");

            await foreach (
                var entity in
                _tableClient.QueryAsync<TableEntity>(
                    filter: filter))
            {
                sensors.Add(
                    MapSensor(entity));
            }

            return sensors;
        }

        // ============================================================
        // GET ONE SENSOR
        // ============================================================

        public async Task<Sensor?> GetSensorAsync(
            string macAddress)
        {
            try
            {
                var entity =
                    await _tableClient.GetEntityAsync<TableEntity>(
                        "Sensor",
                        macAddress);

                return MapSensor(
                    entity.Value);
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }
        }

        // ============================================================
        // CREATE SENSOR
        // ============================================================

        public async Task CreateSensorAsync(
            Sensor sensor)
        {
            var entity =
                new TableEntity(
                    "Sensor",
                    sensor.MacAddress)
                {
                    ["Location"] =
                        sensor.Location,

                    ["Category"] =
                        sensor.Category
                };

            await _tableClient.AddEntityAsync(
                entity);
        }

        // ============================================================
        // UPDATE SENSOR
        // ============================================================

        public async Task<bool> UpdateSensorAsync(
            string macAddress,
            Sensor sensor)
        {
            try
            {
                var entity =
                    new TableEntity(
                        "Sensor",
                        macAddress)
                    {
                        ["Location"] =
                            sensor.Location,

                        ["Category"] =
                            sensor.Category
                    };

                await _tableClient.UpdateEntityAsync(
                    entity,
                    ETag.All,
                    TableUpdateMode.Replace);

                return true;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return false;
            }
        }

        // ============================================================
        // DELETE SENSOR
        // ============================================================

        public async Task<bool> DeleteSensorAsync(
            string macAddress)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(
                    "Sensor",
                    macAddress);

                return true;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return false;
            }
        }

        // ============================================================
        // SENSOR MAPPING
        // ============================================================

        private static Sensor MapSensor(
            TableEntity entity)
        {
            return new Sensor
            {
                MacAddress =
                    entity.RowKey,

                Location =
                    entity.GetString(
                        "Location")
                    ?? string.Empty,

                Category =
                    entity.GetString(
                        "Category")
                    ?? string.Empty
            };
        }
    }
}
