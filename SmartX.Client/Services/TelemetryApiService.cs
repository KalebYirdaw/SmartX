
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

        // Existing telemetry methods

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

        // Network topology methods

        public async Task<NetworkTopologyDto?> GetNetworkTopologyAsync()
        {
            return await _httpClient.GetFromJsonAsync<NetworkTopologyDto>(
                "api/network-topology");
        }

        public async Task<List<NetworkGraphNodeDto>> GetNetworkNodesAsync()
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<NetworkGraphNodeDto>>(
                    "api/network-topology/nodes");

            return result ?? new List<NetworkGraphNodeDto>();
        }

        public async Task<List<NetworkGraphEdgeDto>> GetNetworkEdgesAsync()
        {
            var result =
                await _httpClient.GetFromJsonAsync<List<NetworkGraphEdgeDto>>(
                    "api/network-topology/edges");

            return result ?? new List<NetworkGraphEdgeDto>();
        }

        public async Task<NetworkTraversalDto?> GetBreadthFirstSearchAsync(
            string macAddress)
        {
            return await _httpClient.GetFromJsonAsync<NetworkTraversalDto>(
                $"api/network-topology/bfs/{Uri.EscapeDataString(macAddress)}");
        }

        public async Task<NetworkTraversalDto?> GetDepthFirstSearchAsync(
            string macAddress)
        {
            return await _httpClient.GetFromJsonAsync<NetworkTraversalDto>(
                $"api/network-topology/dfs/{Uri.EscapeDataString(macAddress)}");
        }

        public async Task<MinimumSpanningTreeDto?> GetPrimMstAsync()
        {
            return await _httpClient.GetFromJsonAsync<MinimumSpanningTreeDto>(
                "api/network-topology/mst/prim");
        }

        public async Task<MinimumSpanningTreeDto?> GetKruskalMstAsync()
        {
            return await _httpClient.GetFromJsonAsync<MinimumSpanningTreeDto>(
                "api/network-topology/mst/kruskal");
        }
    }

    // Existing telemetry response classes

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

    // Network topology response classes

    public class NetworkTopologyDto
    {
        public int NodeCount { get; set; }
        public int EdgeCount { get; set; }
        public int OnlineNodeCount { get; set; }
        public int OfflineNodeCount { get; set; }

        public List<NetworkGraphNodeDto> Nodes { get; set; } = new();
        public List<NetworkGraphEdgeDto> Edges { get; set; } = new();
    }

    public class NetworkGraphNodeDto
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeenUtc { get; set; }
    }

    public class NetworkGraphEdgeDto
    {
        public string SourceMacAddress { get; set; } = string.Empty;
        public string TargetMacAddress { get; set; } = string.Empty;
        public double Weight { get; set; }
        public double SignalStrength { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
    }

    // BFS and DFS response classes

    public class NetworkTraversalDto
    {
        public string Algorithm { get; set; } = string.Empty;
        public string StartMacAddress { get; set; } = string.Empty;

        public int TotalDeviceCount { get; set; }
        public int VisitedCount { get; set; }
        public int UnreachableCount { get; set; }

        public bool IsFullyConnected { get; set; }

        public List<NetworkTraversalEntryDto> TraversalOrder { get; set; } = new();
        public List<NetworkUnreachableDeviceDto> UnreachableDevices { get; set; } = new();
    }

    public class NetworkTraversalEntryDto
    {
        public string MacAddress { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Depth { get; set; }
        public bool IsOnline { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;

        public int TraversalDistance =>
            Level != 0 ? Level : Depth;
    }

    public class NetworkUnreachableDeviceDto
    {
        public string MacAddress { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    // Prim and Kruskal response classes

    public class MinimumSpanningTreeDto
    {
        public string Algorithm { get; set; } = string.Empty;

        public int TotalDeviceCount { get; set; }
        public int OnlineDeviceCount { get; set; }
        public int OfflineDeviceCount { get; set; }

        public int SelectedConnectionCount { get; set; }
        public int ComponentCount { get; set; }

        public bool IsFullyConnected { get; set; }

        public double TotalWeight { get; set; }

        public List<NetworkGraphEdgeDto> SelectedEdges { get; set; } = new();
        public List<NetworkUnreachableDeviceDto> OfflineDevices { get; set; } = new();
    }
}
