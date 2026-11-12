using SmartX.Api.Models;

namespace SmartX.Api.DataStructures;

/// <summary>
/// Undirected weighted graph representing the Smart-X wireless mesh.
/// Nodes represent devices and edges represent wireless connections.
/// </summary>
public sealed class TelemetryNetworkGraph
{
    private readonly object _sync = new();

    private readonly Dictionary<string, NetworkGraphNode> _nodes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, NetworkGraphEdge>>
        _adjacency = new(StringComparer.OrdinalIgnoreCase);

    public void AddOrUpdateNode(NetworkGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (string.IsNullOrWhiteSpace(node.MacAddress))
            throw new ArgumentException("A MAC address is required.");

        lock (_sync)
        {
            var mac = Normalize(node.MacAddress);

            node.MacAddress = mac;

            if (_nodes.TryGetValue(mac, out var existingNode))
            {
                node.IsOnline = existingNode.IsOnline;
                node.LastSeenUtc = existingNode.LastSeenUtc;
            }

            _nodes[mac] = node;

            if (!_adjacency.ContainsKey(mac))
            {
                _adjacency[mac] =
                    new Dictionary<string, NetworkGraphEdge>(
                        StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public bool AddOrUpdateEdge(NetworkGraphEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);

        if (
            string.IsNullOrWhiteSpace(edge.SourceMacAddress) ||
            string.IsNullOrWhiteSpace(edge.TargetMacAddress))
        {
            throw new ArgumentException(
                "Both source and target MAC addresses are required.");
        }

        if (
            !double.IsFinite(edge.Weight) ||
            edge.Weight <= 0)
        {
            throw new ArgumentException(
                "Edge weight must be a positive finite number.");
        }

        if (!double.IsFinite(edge.SignalStrength))
        {
            throw new ArgumentException(
                "Signal strength must be a finite number.");
        }

        var source = Normalize(edge.SourceMacAddress);
        var target = Normalize(edge.TargetMacAddress);

        if (source == target)
        {
            throw new ArgumentException(
                "A device cannot connect to itself.");
        }

        lock (_sync)
        {
            if (
                !_nodes.ContainsKey(source) ||
                !_nodes.ContainsKey(target))
            {
                return false;
            }

            edge.SourceMacAddress = source;
            edge.TargetMacAddress = target;
            edge.LastUpdatedUtc = DateTime.UtcNow;

            // Store the same connection in both directions.
            _adjacency[source][target] = edge;
            _adjacency[target][source] = edge;

            return true;
        }
    }

    public bool RemoveEdge(string sourceMac, string targetMac)
    {
        var source = Normalize(sourceMac);
        var target = Normalize(targetMac);

        lock (_sync)
        {
            if (
                !_adjacency.ContainsKey(source) ||
                !_adjacency.ContainsKey(target))
            {
                return false;
            }

            var removedForward =
                _adjacency[source].Remove(target);

            var removedReverse =
                _adjacency[target].Remove(source);

            return removedForward || removedReverse;
        }
    }

    public bool RemoveNode(string macAddress)
    {
        var mac = Normalize(macAddress);

        lock (_sync)
        {
            if (!_nodes.ContainsKey(mac))
                return false;

            foreach (var neighbour in _adjacency[mac].Keys.ToList())
            {
                _adjacency[neighbour].Remove(mac);
            }

            _adjacency.Remove(mac);
            _nodes.Remove(mac);

            return true;
        }
    }

    public bool SetNodeStatus(string macAddress, bool isOnline)
    {
        var mac = Normalize(macAddress);

        lock (_sync)
        {
            if (!_nodes.TryGetValue(mac, out var node))
                return false;

            node.IsOnline = isOnline;
            node.LastSeenUtc = DateTime.UtcNow;

            return true;
        }
    }

    public NetworkGraphNode? GetNode(string macAddress)
    {
        var mac = Normalize(macAddress);

        lock (_sync)
        {
            if (!_nodes.TryGetValue(mac, out var node))
                return null;

            return CopyNode(node);
        }
    }

    public List<NetworkGraphNode> GetNodes()
    {
        lock (_sync)
        {
            return _nodes.Values
                .Select(CopyNode)
                .ToList();
        }
    }

    public List<NetworkGraphEdge> GetEdges()
    {
        lock (_sync)
        {
            return _adjacency
                .SelectMany(source =>
                    source.Value.Select(target => new
                    {
                        Source = source.Key,
                        Target = target.Key,
                        Edge = target.Value
                    }))
                .Where(item =>
                    string.CompareOrdinal(
                        item.Source,
                        item.Target) < 0)
                .Select(item => CopyEdge(item.Edge))
                .ToList();
        }
    }

    public List<NetworkGraphEdge> GetNeighbours(string macAddress)
    {
        var mac = Normalize(macAddress);

        lock (_sync)
        {
            if (!_adjacency.TryGetValue(mac, out var neighbours))
                return new List<NetworkGraphEdge>();

            return neighbours.Values
                .Select(CopyEdge)
                .ToList();
        }
    }

    /// <summary>
    /// Traverses active connections breadth-first, forwarding only through online devices.
    /// </summary>
    public object TraverseBreadthFirst(string startMacAddress)
    {
        var start = Normalize(startMacAddress);

        lock (_sync)
        {
            if (!_nodes.ContainsKey(start))
                throw new KeyNotFoundException("Starting device was not found.");

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                start
            };
            var queue = new Queue<(string MacAddress, int Level)>();
            var traversalOrder = new List<object>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (currentMac, level) = queue.Dequeue();
                var currentNode = _nodes[currentMac];

                traversalOrder.Add(new
                {
                    MacAddress = currentMac,
                    Level = level,
                    IsOnline = currentNode.IsOnline,
                    Location = currentNode.Location,
                    Category = currentNode.Category
                });

                if (!currentNode.IsOnline ||
                    !_adjacency.TryGetValue(currentMac, out var neighbours))
                {
                    continue;
                }

                foreach (var neighbour in neighbours
                             .Where(item =>
                                 item.Value.IsActive &&
                                 _nodes.TryGetValue(item.Key, out var node) &&
                                 node.IsOnline)
                             .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (visited.Add(neighbour.Key))
                        queue.Enqueue((neighbour.Key, level + 1));
                }
            }

            return BuildTraversalResult("BFS", start, visited, traversalOrder);
        }
    }

    /// <summary>
    /// Traverses active connections depth-first, forwarding only through online devices.
    /// </summary>
    public object TraverseDepthFirst(string startMacAddress)
    {
        var start = Normalize(startMacAddress);

        lock (_sync)
        {
            if (!_nodes.ContainsKey(start))
                throw new KeyNotFoundException("Starting device was not found.");

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<(string MacAddress, int Depth)>();
            var traversalOrder = new List<object>();
            stack.Push((start, 0));

            while (stack.Count > 0)
            {
                var (currentMac, depth) = stack.Pop();

                if (!visited.Add(currentMac))
                    continue;

                var currentNode = _nodes[currentMac];

                traversalOrder.Add(new
                {
                    MacAddress = currentMac,
                    Depth = depth,
                    IsOnline = currentNode.IsOnline,
                    Location = currentNode.Location,
                    Category = currentNode.Category
                });

                if (!currentNode.IsOnline ||
                    !_adjacency.TryGetValue(currentMac, out var neighbours))
                {
                    continue;
                }

                foreach (var neighbour in neighbours
                             .Where(item =>
                                 item.Value.IsActive &&
                                 _nodes.TryGetValue(item.Key, out var node) &&
                                 node.IsOnline &&
                                 !visited.Contains(item.Key))
                             .OrderByDescending(
                                 item => item.Key,
                                 StringComparer.OrdinalIgnoreCase))
                {
                    stack.Push((neighbour.Key, depth + 1));
                }
            }

            return BuildTraversalResult("DFS", start, visited, traversalOrder);
        }
    }

    private object BuildTraversalResult(
        string algorithm,
        string startMac,
        HashSet<string> visited,
        List<object> traversalOrder)
    {
        var unreachableDevices = _nodes.Values
            .Where(node => !visited.Contains(node.MacAddress))
            .OrderBy(node => node.MacAddress, StringComparer.OrdinalIgnoreCase)
            .Select(node => new
            {
                MacAddress = node.MacAddress,
                IsOnline = node.IsOnline,
                Location = node.Location,
                Category = node.Category
            })
            .ToList();

        return new
        {
            Algorithm = algorithm,
            StartMacAddress = startMac,
            TotalDeviceCount = _nodes.Count,
            VisitedCount = visited.Count,
            UnreachableCount = unreachableDevices.Count,
            IsFullyConnected = visited.Count == _nodes.Count,
            TraversalOrder = traversalOrder,
            UnreachableDevices = unreachableDevices
        };
    }

    /// <summary>
    /// Prim's algorithm.
    /// Builds a minimum spanning forest using the lowest-cost active
    /// connections between online devices.
    /// </summary>
    public object CalculatePrimMinimumSpanningTree()
    {
        lock (_sync)
        {
            var onlineNodes = _nodes.Values
                .Where(node => node.IsOnline)
                .OrderBy(node => node.MacAddress, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var visited = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            var selectedEdges = new List<NetworkGraphEdge>();
            var componentCount = 0;

            foreach (var startNode in onlineNodes)
            {
                if (visited.Contains(startNode.MacAddress))
                    continue;

                componentCount++;
                visited.Add(startNode.MacAddress);

                var queue = new PriorityQueue<
                    NetworkGraphEdge,
                    (double Weight, string Source, string Target)>();

                AddAvailableEdges(startNode.MacAddress, queue, visited);

                while (queue.Count > 0)
                {
                    var edge = queue.Dequeue();

                    if (visited.Contains(edge.TargetMacAddress))
                        continue;

                    selectedEdges.Add(CopyEdge(edge));
                    visited.Add(edge.TargetMacAddress);
                    AddAvailableEdges(
                        edge.TargetMacAddress,
                        queue,
                        visited);
                }
            }

            return BuildMinimumSpanningResult(
                "Prim",
                selectedEdges,
                onlineNodes,
                componentCount);
        }
    }

    /// <summary>
    /// Adds active connections from a visited device to unvisited
    /// online devices into Prim's priority queue.
    /// </summary>
    private void AddAvailableEdges(
        string currentMac,
        PriorityQueue<
            NetworkGraphEdge,
            (double Weight, string Source, string Target)> queue,
        HashSet<string> visited)
    {
        if (!_adjacency.TryGetValue(currentMac, out var neighbours))
            return;

        foreach (var neighbour in neighbours
                     .Where(item =>
                         item.Value.IsActive &&
                         !visited.Contains(item.Key) &&
                         _nodes.TryGetValue(item.Key, out var node) &&
                         node.IsOnline)
                     .OrderBy(item => item.Value.Weight)
                     .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            var candidate = CopyEdge(neighbour.Value);
            candidate.SourceMacAddress = currentMac;
            candidate.TargetMacAddress = neighbour.Key;

            queue.Enqueue(
                candidate,
                (
                    candidate.Weight,
                    candidate.SourceMacAddress,
                    candidate.TargetMacAddress
                ));
        }
    }

    /// <summary>
    /// Kruskal's algorithm.
    /// Sorts active connections by weight and uses
    /// Disjoint Set Union to prevent cycles.
    /// </summary>
    public object CalculateKruskalMinimumSpanningTree()
    {
        lock (_sync)
        {
            var onlineNodes = _nodes.Values
                .Where(node => node.IsOnline)
                .OrderBy(node => node.MacAddress, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var parent = onlineNodes.ToDictionary(
                node => node.MacAddress,
                node => node.MacAddress,
                StringComparer.OrdinalIgnoreCase);

            var rank = onlineNodes.ToDictionary(
                node => node.MacAddress,
                _ => 0,
                StringComparer.OrdinalIgnoreCase);

            var selectedEdges = new List<NetworkGraphEdge>();
            var componentCount = onlineNodes.Count;

            var availableEdges = _adjacency
                .SelectMany(source =>
                    source.Value.Select(target => new
                    {
                        Source = source.Key,
                        Target = target.Key,
                        Edge = target.Value
                    }))
                .Where(item =>
                    string.CompareOrdinal(item.Source, item.Target) < 0 &&
                    item.Edge.IsActive &&
                    _nodes.TryGetValue(item.Source, out var sourceNode) &&
                    sourceNode.IsOnline &&
                    _nodes.TryGetValue(item.Target, out var targetNode) &&
                    targetNode.IsOnline)
                .OrderBy(item => item.Edge.Weight)
                .ThenBy(item => item.Source, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Target, StringComparer.OrdinalIgnoreCase)
                .ToList();

            string Find(string mac)
            {
                if (!parent.TryGetValue(mac, out var parentMac))
                {
                    throw new InvalidOperationException(
                        "Device is not part of the online network.");
                }

                if (!string.Equals(
                        parentMac,
                        mac,
                        StringComparison.OrdinalIgnoreCase))
                {
                    parent[mac] = Find(parentMac);
                }

                return parent[mac];
            }

            bool Union(string first, string second)
            {
                var rootFirst = Find(first);
                var rootSecond = Find(second);

                if (string.Equals(
                        rootFirst,
                        rootSecond,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (rank[rootFirst] < rank[rootSecond])
                {
                    parent[rootFirst] = rootSecond;
                }
                else if (rank[rootFirst] > rank[rootSecond])
                {
                    parent[rootSecond] = rootFirst;
                }
                else
                {
                    parent[rootSecond] = rootFirst;
                    rank[rootFirst]++;
                }

                return true;
            }

            foreach (var item in availableEdges)
            {
                if (!Union(item.Source, item.Target))
                    continue;

                selectedEdges.Add(CopyEdge(item.Edge));
                componentCount--;

                if (componentCount <= 1)
                    break;
            }

            return BuildMinimumSpanningResult(
                "Kruskal",
                selectedEdges,
                onlineNodes,
                componentCount);
        }
    }

    /// <summary>
    /// Creates a consistent response for both minimum spanning forest algorithms.
    /// </summary>
    private object BuildMinimumSpanningResult(
        string algorithm,
        List<NetworkGraphEdge> selectedEdges,
        List<NetworkGraphNode> onlineNodes,
        int componentCount)
    {
        var offlineDevices = _nodes.Values
            .Where(node => !node.IsOnline)
            .OrderBy(node => node.MacAddress, StringComparer.OrdinalIgnoreCase)
            .Select(node => new
            {
                MacAddress = node.MacAddress,
                Location = node.Location,
                Category = node.Category,
                IsOnline = node.IsOnline
            })
            .ToList();

        return new
        {
            Algorithm = algorithm,
            TotalDeviceCount = _nodes.Count,
            OnlineDeviceCount = onlineNodes.Count,
            OfflineDeviceCount = offlineDevices.Count,
            SelectedConnectionCount = selectedEdges.Count,
            ComponentCount = componentCount,
            IsFullyConnected =
                onlineNodes.Count <= 1 || componentCount == 1,
            TotalWeight = selectedEdges.Sum(edge => edge.Weight),
            SelectedEdges = selectedEdges,
            OfflineDevices = offlineDevices
        };
    }

    public object GetTopologySnapshot()
    {
        lock (_sync)
        {
            var edges = _adjacency
                .SelectMany(source =>
                    source.Value.Select(target => new
                    {
                        Source = source.Key,
                        Target = target.Key,
                        Edge = target.Value
                    }))
                .Where(item =>
                    string.CompareOrdinal(
                        item.Source,
                        item.Target) < 0)
                .Select(item => CopyEdge(item.Edge))
                .ToList();

            return new
            {
                NodeCount = _nodes.Count,
                EdgeCount = edges.Count,
                OnlineNodeCount = _nodes.Values.Count(n => n.IsOnline),
                OfflineNodeCount = _nodes.Values.Count(n => !n.IsOnline),
                Nodes = _nodes.Values.Select(CopyNode).ToList(),
                Edges = edges
            };
        }
    }

    private static string Normalize(string macAddress)
    {
        return macAddress.Trim().ToUpperInvariant();
    }

    private static NetworkGraphNode CopyNode(NetworkGraphNode node)
    {
        return new NetworkGraphNode
        {
            MacAddress = node.MacAddress,
            Location = node.Location,
            Category = node.Category,
            IsOnline = node.IsOnline,
            LastSeenUtc = node.LastSeenUtc
        };
    }

    private static NetworkGraphEdge CopyEdge(NetworkGraphEdge edge)
    {
        return new NetworkGraphEdge
        {
            SourceMacAddress = edge.SourceMacAddress,
            TargetMacAddress = edge.TargetMacAddress,
            Weight = edge.Weight,
            SignalStrength = edge.SignalStrength,
            IsActive = edge.IsActive,
            LastUpdatedUtc = edge.LastUpdatedUtc
        };
    }
}
