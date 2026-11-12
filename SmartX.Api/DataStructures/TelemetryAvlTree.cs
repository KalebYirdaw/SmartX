
using SmartX.Api.Models;

namespace SmartX.Api.DataStructures;

/// <summary>
/// AVL tree indexed by UTC timestamp. Multiple telemetry readings may share
/// the same timestamp, so each node stores a list of readings.
/// </summary>
public sealed class TelemetryAvlTree
{
    private sealed class Node
    {
        public DateTime Timestamp { get; set; }
        public List<Telemetry> Readings { get; } = new();
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Height { get; set; } = 1;

        public Node(DateTime timestamp, Telemetry reading)
        {
            Timestamp = timestamp;
            Readings.Add(reading);
        }
    }

    private Node? _root;

    /// <summary>
    /// Adds a telemetry reading using its UTC timestamp.
    /// </summary>
    public void Add(DateTime timestampUtc, Telemetry reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var key = timestampUtc.Kind == DateTimeKind.Utc
            ? timestampUtc
            : timestampUtc.ToUniversalTime();

        _root = Insert(_root, key, reading);
    }

    /// <summary>
    /// Returns all readings in the inclusive UTC timestamp range,
    /// ordered from oldest to newest.
    /// </summary>
    public List<Telemetry> GetRange(DateTime fromUtc, DateTime toUtc)
    {
        var from = fromUtc.Kind == DateTimeKind.Utc
            ? fromUtc
            : fromUtc.ToUniversalTime();

        var to = toUtc.Kind == DateTimeKind.Utc
            ? toUtc
            : toUtc.ToUniversalTime();

        if (from > to)
        {
            throw new ArgumentException(
                "The start of the range must be earlier than or equal to its end.");
        }

        var results = new List<Telemetry>();

        CollectRange(_root, from, to, results);

        return results;
    }

    /// <summary>
    /// Removes and returns the oldest single reading.
    /// </summary>
    public Telemetry? RemoveOldest()
    {
        if (_root is null)
            return null;

        var oldest = FindMinimum(_root);

        var reading = oldest.Readings[0];

        oldest.Readings.RemoveAt(0);

        if (oldest.Readings.Count == 0)
        {
            _root = RemoveNode(_root, oldest.Timestamp);
        }

        return reading;
    }

    private static Node Insert(
        Node? node,
        DateTime timestamp,
        Telemetry reading)
    {
        if (node is null)
            return new Node(timestamp, reading);

        if (timestamp < node.Timestamp)
        {
            node.Left = Insert(node.Left, timestamp, reading);
        }
        else if (timestamp > node.Timestamp)
        {
            node.Right = Insert(node.Right, timestamp, reading);
        }
        else
        {
            node.Readings.Add(reading);
            return node;
        }

        return Balance(node);
    }

    private static void CollectRange(
        Node? node,
        DateTime from,
        DateTime to,
        List<Telemetry> results)
    {
        if (node is null)
            return;

        if (from < node.Timestamp)
        {
            CollectRange(node.Left, from, to, results);
        }

        if (node.Timestamp >= from && node.Timestamp <= to)
        {
            results.AddRange(node.Readings);
        }

        if (to > node.Timestamp)
        {
            CollectRange(node.Right, from, to, results);
        }
    }

    private static Node FindMinimum(Node node)
    {
        while (node.Left is not null)
        {
            node = node.Left;
        }

        return node;
    }

    private static Node? RemoveNode(Node? node, DateTime timestamp)
    {
        if (node is null)
            return null;

        if (timestamp < node.Timestamp)
        {
            node.Left = RemoveNode(node.Left, timestamp);
        }
        else if (timestamp > node.Timestamp)
        {
            node.Right = RemoveNode(node.Right, timestamp);
        }
        else
        {
            if (node.Left is null)
                return node.Right;

            if (node.Right is null)
                return node.Left;

            var successor = FindMinimum(node.Right);

            node.Timestamp = successor.Timestamp;

            node.Readings.Clear();
            node.Readings.AddRange(successor.Readings);

            node.Right = RemoveWholeNode(
                node.Right,
                successor.Timestamp);
        }

        return Balance(node);
    }

    private static Node? RemoveWholeNode(
        Node? node,
        DateTime timestamp)
    {
        if (node is null)
            return null;

        if (timestamp < node.Timestamp)
        {
            node.Left = RemoveWholeNode(node.Left, timestamp);
        }
        else if (timestamp > node.Timestamp)
        {
            node.Right = RemoveWholeNode(node.Right, timestamp);
        }
        else
        {
            return node.Right;
        }

        return Balance(node);
    }

    private static Node Balance(Node node)
    {
        UpdateHeight(node);

        var balance = GetBalance(node);

        // Left-heavy tree
        if (balance > 1)
        {
            if (GetBalance(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }

            return RotateRight(node);
        }

        // Right-heavy tree
        if (balance < -1)
        {
            if (GetBalance(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static Node RotateRight(Node y)
    {
        var x = y.Left!;
        var transfer = x.Right;

        x.Right = y;
        y.Left = transfer;

        UpdateHeight(y);
        UpdateHeight(x);

        return x;
    }

    private static Node RotateLeft(Node x)
    {
        var y = x.Right!;
        var transfer = y.Left;

        y.Left = x;
        x.Right = transfer;

        UpdateHeight(x);
        UpdateHeight(y);

        return y;
    }

    private static void UpdateHeight(Node node)
    {
        node.Height = 1 + Math.Max(
            Height(node.Left),
            Height(node.Right));
    }

    private static int Height(Node? node)
    {
        return node?.Height ?? 0;
    }

    private static int GetBalance(Node node)
    {
        return Height(node.Left) - Height(node.Right);
    }
}
