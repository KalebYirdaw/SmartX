using System.Net.Http.Json;

namespace SmartX.Client.Services
{
    public class SensorApiService
    {
        private readonly HttpClient _httpClient;

        public SensorApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<SensorDto>> GetSensorsAsync()
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<SensorDto>>(
                    "api/Sensor");

            return result ?? new List<SensorDto>();
        }
    }

    public class SensorDto
    {
        public string MacAddress { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}