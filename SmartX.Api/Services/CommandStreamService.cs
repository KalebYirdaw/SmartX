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
    private readonly PriorityQueue<Telemetry, (int Severity, long Sequence)> _criticalQueue = new();
    private readonly Stack<DeviceCommand> _commandStack = new();
    private readonly Dictionary<string, string> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<DateTime, List<Telemetry>> _history = new();
    private readonly HashSet<string> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime Timestamp, bool IsActive)> _latestAlertStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<DeviceCommand> _commands = new();
    private readonly List<SuggestedAction> _suggestions = new();
    private readonly Dictionary<string, (string Metric, DateTime Timestamp)> _recentCriticalReadings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (DateTime Timestamp, bool IsCritical)> _latestSensorReadings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string Command, string Value, int Count)> _commandPatterns = new(StringComparer.OrdinalIgnoreCase);
    private long _enqueueSequence;

    public object Snapshot()
    {
        lock (_sync)
        {
            return new
            {
                incomingQueueCount = _incomingQueue.Count,
                criticalQueueCount = _criticalQueue.Count,
                registeredDeviceCount = _devices.Count,
                activeAlerts = _activeAlerts.ToArray(),
                recentCommands = _commands.OrderByDescending(c => c.CreatedAtUtc).Take(30).ToList(),
                historyCount = _history.Sum(pair => pair.Value.Count),
                suggestions = _suggestions.ToList(),
                commandPatternCount = _commandPatterns.Values.Sum(pattern => pattern.Count)
            };
        }
    }

    public void RegisterDevice(string mac, string category)
    {
        lock (_sync) _devices[mac] = category;
    }

    public void UnregisterDevice(string mac)
    {
        lock (_sync) _devices.Remove(mac);
    }

    public bool TryGetDeviceCategory(string mac, out string category)
    {
        lock (_sync) return _devices.TryGetValue(mac, out category!);
    }

    public void EnqueueTelemetry(Telemetry reading)
    {
        lock (_sync)
        {
            bool critical = (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
                || (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase) && reading.Value > 1500)
                || (reading.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase) && (reading.Value < 15 || reading.Value > 35));

            if (critical) _criticalQueue.Enqueue(reading, (GetCriticalPriority(reading), _enqueueSequence++));
            else _incomingQueue.Enqueue(reading);
        }
    }

    public QueueProcessingResult ProcessQueuedTelemetry()
    {
        lock (_sync)
        {
            var processed = new List<ProcessedTelemetry>();

            while (_criticalQueue.TryDequeue(out var critical, out _))
            {
                StoreHistory(critical);
                processed.Add(new ProcessedTelemetry(critical.SensorMacAddress, critical.Metric, "Critical"));
            }

            while (_incomingQueue.TryDequeue(out var normal))
            {
                StoreHistory(normal);
                processed.Add(new ProcessedTelemetry(normal.SensorMacAddress, normal.Metric, "FIFO"));
            }

            return new QueueProcessingResult(
                processed.Count(item => item.Queue == "Critical"),
                processed.Count(item => item.Queue == "FIFO"),
                processed);
        }
    }

    private void StoreHistory(Telemetry reading)
    {
        var key = reading.Timestamp == default ? DateTime.UtcNow : reading.Timestamp;
        if (!_history.TryGetValue(key, out var readings)) _history[key] = readings = new List<Telemetry>();
        readings.Add(reading);

        var alertKey = $"{reading.SensorMacAddress}:{reading.Metric}:critical";
        bool isCritical = IsCritical(reading);
        if (!_latestAlertStates.TryGetValue(alertKey, out var latest) || key >= latest.Timestamp)
        {
            _latestAlertStates[alertKey] = (key, isCritical);
            if (isCritical) _activeAlerts.Add(alertKey);
            else _activeAlerts.Remove(alertKey);
        }

        UpdateInsight(reading);

        if (!_latestSensorReadings.TryGetValue(reading.SensorMacAddress, out var latestSensorReading) ||
            key >= latestSensorReading.Timestamp)
        {
            _latestSensorReadings[reading.SensorMacAddress] = (key, isCritical);
            if (isCritical)
                _recentCriticalReadings[reading.SensorMacAddress] = (GetMetricFamily(reading.Metric), key);
            else
                _recentCriticalReadings.Remove(reading.SensorMacAddress);
        }
    }

    private void UpdateInsight(Telemetry reading)
    {
        _suggestions.RemoveAll(s => s.SensorMacAddress.Equals(reading.SensorMacAddress, StringComparison.OrdinalIgnoreCase));

        var pattern = _commandPatterns
            .Where(pair => pair.Key.StartsWith($"{GetMetricFamily(reading.Metric)}|", StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .OrderByDescending(value => value.Count)
            .FirstOrDefault();

        if (pattern.Count > 0 && IsCritical(reading))
        {
            _suggestions.Add(new SuggestedAction
            {
                SensorMacAddress = reading.SensorMacAddress,
                Title = $"Based on operator history: {pattern.Command}",
                Reason = $"Operators used {pattern.Command} {pattern.Value} after {pattern.Count} previous critical {GetMetricFamily(reading.Metric)} reading(s). Review this suggested response.",
                SuggestedCommand = pattern.Command,
                SuggestedValue = pattern.Value,
                Priority = "High",
                BasedOnHistory = true,
                EvidenceCount = pattern.Count
            });
        }
        else if (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
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

            if (_recentCriticalReadings.TryGetValue(request.SensorMacAddress, out var recent) &&
                DateTime.UtcNow - recent.Timestamp <= TimeSpan.FromMinutes(30))
            {
                var patternKey = $"{recent.Metric}|{request.Command}|{request.Value}";
                if (_commandPatterns.TryGetValue(patternKey, out var pattern))
                    _commandPatterns[patternKey] = (request.Command, request.Value, pattern.Count + 1);
                else
                    _commandPatterns[patternKey] = (request.Command, request.Value, 1);
            }

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

    private static bool IsCritical(Telemetry reading) =>
        (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase) && reading.Value < 20)
        || (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase) && reading.Value > 1500)
        || (reading.Metric.Contains("temperature", StringComparison.OrdinalIgnoreCase) && (reading.Value < 15 || reading.Value > 35));

    private static string GetMetricFamily(string metric)
    {
        if (metric.Contains("moisture", StringComparison.OrdinalIgnoreCase)) return "moisture";
        if (metric.Contains("power", StringComparison.OrdinalIgnoreCase)) return "power";
        if (metric.Contains("temperature", StringComparison.OrdinalIgnoreCase)) return "temperature";
        return metric.Trim().ToLowerInvariant();
    }

    private static int GetCriticalPriority(Telemetry reading)
    {
        if (reading.Metric.Contains("moisture", StringComparison.OrdinalIgnoreCase))
            return Math.Clamp((int)Math.Floor(reading.Value), 0, 19);

        if (reading.Metric.Contains("power", StringComparison.OrdinalIgnoreCase))
            return -(int)Math.Min(Math.Ceiling(reading.Value - 1500), int.MaxValue);

        var deviationFromNormal = Math.Abs(reading.Value - 25);
        return -(int)Math.Min(Math.Ceiling(deviationFromNormal), int.MaxValue);
    }
}

public sealed record ProcessedTelemetry(string SensorMacAddress, string Metric, string Queue);

public sealed record QueueProcessingResult(
    int CriticalProcessed,
    int FifoProcessed,
    IReadOnlyList<ProcessedTelemetry> ProcessingOrder);
