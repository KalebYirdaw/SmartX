using System.Collections.Concurrent;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

/// <summary>
/// In-memory demonstration of common IoT processing structures:
/// FIFO queue, priority queue, device dictionary, sorted history,
/// unique active alerts, and command stack for Undo.
/// </summary>
public class CommandStreamService
{
    private readonly object _sync = new();
    private readonly Queue<Telemetry> _incomingQueue = new();
    private readonly PriorityQueue<Telemetry, int> _criticalQueue = new();
    private readonly Stack<DeviceCommand> _commandStack = new();
    private readonly Dictionary<string, string> _deviceStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<DateTime, List<Telemetry>> _history = new();
    private readonly HashSet<string> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DeviceCommand> _commands = new();
    private readonly List<SuggestedAction> _suggestions = new();

    public object Snapshot()
    {
        lock (_sync)
        {
            return new
            {
                incomingQueueCount = _incomingQueue.Count,
                criticalQueueCount = _criticalQueue.Count,
                registeredDeviceCount = _deviceStates.Count,
                activeAlerts = _activeAlerts.ToArray(),
                recentCommands = _commands.OrderByDescending(c => c.CreatedAtUtc).Take(30).ToList(),
                historyCount = _history.Sum(pair => pair.Value.Count),
                suggestions = _suggestions.ToList()
            };
        }
    }

    public void RegisterDevice(string mac, string category)
    {
        lock (_sync) _deviceStates[mac] = category;
    }

    public void EnqueueTelemetry(Telemetry reading)
    {
        lock (_sync)
        {
            bool critical = (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
                || (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase) && reading.Value > 1500)
                || (reading.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase) && (reading.Value < 15 || reading.Value > 35));

            if (critical) _criticalQueue.Enqueue(reading, 0);
            else _incomingQueue.Enqueue(reading);
            ProcessQueues();
            UpdateInsight(reading);
        }
    }

    private void ProcessQueues()
    {
        // Critical items are consumed first, then normal FIFO items.
        while (_criticalQueue.TryDequeue(out var critical, out _)) StoreHistory(critical);
        while (_incomingQueue.TryDequeue(out var normal)) StoreHistory(normal);
    }

    private void StoreHistory(Telemetry reading)
    {
        var key = reading.Timestamp == default ? DateTime.UtcNow : reading.Timestamp;
        if (!_history.TryGetValue(key, out var readings)) _history[key] = readings = new List<Telemetry>();
        readings.Add(reading);

        var alertKey = $"{reading.SensorMacAddress}:{reading.Metric}:critical";
        bool isCritical = (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
            || (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase) && reading.Value > 1500)
            || (reading.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase) && (reading.Value < 15 || reading.Value > 35));
        if (isCritical) _activeAlerts.Add(alertKey);
        else _activeAlerts.Remove(alertKey);
    }

    private void UpdateInsight(Telemetry reading)
    {
        _suggestions.RemoveAll(s => s.SensorMacAddress.Equals(reading.SensorMacAddress, StringComparison.OrdinalIgnoreCase));
        if (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
        {
            _suggestions.Add(new SuggestedAction { SensorMacAddress = reading.SensorMacAddress, Title = "Check irrigation", Reason = $"Moisture is low ({reading.Value}). Inspect the sensor and consider activating the water pump.", SuggestedCommand = "Water Pump", SuggestedValue = "ON", Priority = "Critical" });
        }
        else if (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase) && reading.Value > 1500)
        {
            _suggestions.Add(new SuggestedAction { SensorMacAddress = reading.SensorMacAddress, Title = "Investigate power spike", Reason = $"Power reading is high ({reading.Value}). Check the connected load before overriding a relay.", SuggestedCommand = "Smart Relay", SuggestedValue = "OFF", Priority = "High" });
        }
        else if (reading.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase) && (reading.Value < 15 || reading.Value > 35))
        {
            _suggestions.Add(new SuggestedAction { SensorMacAddress = reading.SensorMacAddress, Title = "Inspect temperature sensor", Reason = $"Temperature reading ({reading.Value}) is outside the configured range.", SuggestedCommand = "Request Sensor Check", SuggestedValue = "RUN", Priority = "High" });
        }
    }

    public DeviceCommand IssueCommand(CommandRequest request)
    {
        lock (_sync)
        {
            var command = new DeviceCommand
            {
                SensorMacAddress = request.SensorMacAddress,
                Command = request.Command,
                Value = request.Value,
                CreatedAtUtc = DateTime.UtcNow
            };
            _commands.Add(command);
            _commandStack.Push(command);
            return command;
        }
    }

    public DeviceCommand? UndoLastCommand()
    {
        lock (_sync)
        {
            if (!_commandStack.TryPop(out var last)) return null;
            var undo = new DeviceCommand
            {
                SensorMacAddress = last.SensorMacAddress,
                Command = $"UNDO: {last.Command}",
                Value = last.Value == "ON" ? "OFF" : last.Value == "OFF" ? "ON" : "REVERT",
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Simulated undo"
            };
            _commands.Add(undo);
            return undo;
        }
    }

    public List<Telemetry> GetHistory(int count = 100)
    {
        lock (_sync)
            return _history.SelectMany(pair => pair.Value).OrderByDescending(t => t.Timestamp).Take(Math.Clamp(count, 1, 500)).ToList();
    }

    public List<SuggestedAction> GetSuggestions()
    {
        lock (_sync) return _suggestions.ToList();
    }
}
