
using System.Net.Http.Json;

namespace SmartX.Client.Services
{
    public class TelemetryApiService
    {
        private readonly HttpClient _httpClient;

        public TelemetryApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TelemetryDto>> GetTelemetryAsync(
            string sensorMacAddress)
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<TelemetryDto>>(
                    $"api/Telemetry/{Uri.EscapeDataString(sensorMacAddress)}");

            return result ?? new List<TelemetryDto>();
        }

        public async Task<List<TelemetryAnomalyDto>> GetAnomaliesAsync(
            string sensorMacAddress)
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<TelemetryAnomalyDto>>(
                    $"api/Telemetry/anomalies/{Uri.EscapeDataString(sensorMacAddress)}");

            return result ?? new List<TelemetryAnomalyDto>();
        }

        public async Task<SensorHealthDto?> GetSensorHealthAsync(
            string sensorMacAddress)
        {
            return await _httpClient.GetFromJsonAsync<SensorHealthDto>(
                $"api/SensorHealth/{Uri.EscapeDataString(sensorMacAddress)}");
        }
    }

    public class TelemetryDto
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }
    }

    public class TelemetryAnomalyDto
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }

        public string Reason { get; set; } = string.Empty;
    }

    public class SensorHealthDto
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? LastTelemetryTime { get; set; }

        public double? MinutesSinceLastTelemetry { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}

