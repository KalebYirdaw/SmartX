using SmartX.Api.DataStructures;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public sealed class NetworkTopologyService
{
    private readonly TelemetryNetworkGraph _graph = new();

    public void RegisterSensor(Sensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);

        _graph.AddOrUpdateNode(new NetworkGraphNode
        {
            MacAddress = sensor.MacAddress,
            Location = sensor.Location,
            Category = sensor.Category,
            IsOnline = true,
            LastSeenUtc = DateTime.UtcNow
        });
    }

    public bool RemoveSensor(string macAddress)
    {
        return _graph.RemoveNode(macAddress);
    }

    public bool AddConnection(NetworkGraphEdge edge)
    {
        return _graph.AddOrUpdateEdge(edge);
    }

    public bool RemoveConnection(
        string sourceMacAddress,
        string targetMacAddress)
    {
        return _graph.RemoveEdge(
            sourceMacAddress,
            targetMacAddress);
    }

    public bool UpdateDeviceStatus(
        string macAddress,
        bool isOnline)
    {
        return _graph.SetNodeStatus(macAddress, isOnline);
    }

    public NetworkGraphNode? GetDevice(string macAddress)
    {
        return _graph.GetNode(macAddress);
    }

    public List<NetworkGraphNode> GetDevices()
    {
        return _graph.GetNodes();
    }

    public List<NetworkGraphEdge> GetConnections()
    {
        return _graph.GetEdges();
    }

    public List<NetworkGraphEdge> GetNeighbours(string macAddress)
    {
        return _graph.GetNeighbours(macAddress);
    }

    public object TraverseBreadthFirst(string startMacAddress)
    {
        return _graph.TraverseBreadthFirst(startMacAddress);
    }

    public object TraverseDepthFirst(string startMacAddress)
    {
        return _graph.TraverseDepthFirst(startMacAddress);
    }

    /// <summary>
    /// Calculates the minimum spanning forest using Prim's algorithm.
    /// </summary>
    public object CalculatePrimMinimumSpanningTree()
    {
        return _graph.CalculatePrimMinimumSpanningTree();
    }

    /// <summary>
    /// Calculates the minimum spanning forest using Kruskal's algorithm.
    /// </summary>
    public object CalculateKruskalMinimumSpanningTree()
    {
        return _graph.CalculateKruskalMinimumSpanningTree();
    }

    public object GetTopology()
    {
        return _graph.GetTopologySnapshot();
    }
}
