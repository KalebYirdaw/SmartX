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
    }

    public class TelemetryDto
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }
    }
}