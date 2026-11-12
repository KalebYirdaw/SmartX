
using SmartX.Api.Models;
using SmartX.Api.DataStructures;

namespace SmartX.Api.Services;

/// <summary>
/// In-memory Part 2 processing structures:
/// FIFO queue, critical priority queue, device dictionary,
/// AVL timestamp history, unique alerts and command undo stack.
/// </summary>
public class CommandStreamService
{
    private const int MaximumHistoryReadings = 50_000;
    private const int MinimumLearningEvidence = 3;
    private const double MinimumLearningConfidence = 0.60;

    private readonly object _sync = new();

    private readonly Queue<Telemetry> _incomingQueue = new();

    private readonly PriorityQueue<Telemetry, (double Severity, long Sequence)>
        _criticalQueue = new();

    private readonly Stack<DeviceCommand> _commandStack = new();

    private readonly Dictionary<string, string> _devices =
        new(StringComparer.OrdinalIgnoreCase);

    // AVL tree replaces SortedDictionary for timestamp history.
    private readonly TelemetryAvlTree _history = new();

    private readonly HashSet<string> _activeAlerts =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, (DateTime Timestamp, bool IsActive)>
        _latestAlertStates =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly List<DeviceCommand> _commands = new();

    private readonly List<SuggestedAction> _suggestions = new();

    private readonly Dictionary<string, (string Metric, DateTime Timestamp)>
        _recentCriticalReadings =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, (DateTime Timestamp, bool IsCritical)>
        _latestSensorReadings =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, (string Command, string Value, int Count)>
        _commandPatterns =
            new(StringComparer.OrdinalIgnoreCase);

    private long _enqueueSequence;

    private int _historyCount;

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
                recentCommands = _commands
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .Take(30)
                    .ToList(),
                historyCount = _historyCount,
                historyRetentionLimit = MaximumHistoryReadings,
                suggestions = _suggestions.ToList(),
                commandPatternCount = _commandPatterns.Values
                    .Sum(pattern => pattern.Count)
            };
        }
    }

    public void RegisterDevice(string mac, string category)
    {
        lock (_sync)
        {
            _devices[mac] = category;
        }
    }

    public void UnregisterDevice(string mac)
    {
        lock (_sync)
        {
            _devices.Remove(mac);
        }
    }

    public bool TryGetDeviceCategory(
        string mac,
        out string category)
    {
        lock (_sync)
        {
            return _devices.TryGetValue(mac, out category!);
        }
    }

    public void EnqueueTelemetry(Telemetry reading)
    {
        lock (_sync)
        {
            if (TryGetNormalisedSeverity(reading, out var severity))
            {
                _criticalQueue.Enqueue(
                    reading,
                    (-severity, _enqueueSequence++));
            }
            else
            {
                _incomingQueue.Enqueue(reading);
            }
        }
    }

    /// <summary>
    /// Processes at most maxItems.
    /// Critical readings are processed before normal FIFO readings.
    /// </summary>
    public QueueProcessingResult ProcessQueuedTelemetry(
        int maxItems = 100)
    {
        maxItems = Math.Clamp(maxItems, 1, 1_000);

        lock (_sync)
        {
            var processed = new List<ProcessedTelemetry>(maxItems);

            while (
                processed.Count < maxItems &&
                _criticalQueue.TryDequeue(out var critical, out _))
            {
                StoreHistory(critical);

                processed.Add(
                    new ProcessedTelemetry(
                        critical.SensorMacAddress,
                        critical.Metric,
                        critical.Value,
                        "Critical"));
            }

            while (
                processed.Count < maxItems &&
                _incomingQueue.TryDequeue(out var normal))
            {
                StoreHistory(normal);

                processed.Add(
                    new ProcessedTelemetry(
                        normal.SensorMacAddress,
                        normal.Metric,
                        normal.Value,
                        "FIFO"));
            }

            return new QueueProcessingResult(
                processed.Count(item => item.Queue == "Critical"),
                processed.Count(item => item.Queue == "FIFO"),
                processed);
        }
    }

    /// <summary>
    /// Adds telemetry to the AVL history and removes the oldest
    /// readings when the retention limit is exceeded.
    /// </summary>
    private void StoreHistory(Telemetry reading)
    {
        var key = reading.Timestamp == default
            ? DateTime.UtcNow
            : reading.Timestamp.ToUniversalTime();

        reading.Timestamp = key;

        // Insert into the AVL tree.
        _history.Add(key, reading);

        _historyCount++;

        // Keep only the newest 50,000 readings.
        while (_historyCount > MaximumHistoryReadings)
        {
            if (_history.RemoveOldest() is null)
                break;

            _historyCount--;
        }

        var alertKey =
            $"{reading.SensorMacAddress}:{reading.Metric}:critical";

        bool isCritical = IsCritical(reading);

        if (
            !_latestAlertStates.TryGetValue(alertKey, out var latest) ||
            key >= latest.Timestamp)
        {
            _latestAlertStates[alertKey] = (key, isCritical);

            if (isCritical)
                _activeAlerts.Add(alertKey);
            else
                _activeAlerts.Remove(alertKey);
        }

        UpdateInsight(reading);

        if (
            !_latestSensorReadings.TryGetValue(
                reading.SensorMacAddress,
                out var latestSensorReading) ||
            key >= latestSensorReading.Timestamp)
        {
            _latestSensorReadings[reading.SensorMacAddress] =
                (key, isCritical);

            if (isCritical)
            {
                _recentCriticalReadings[reading.SensorMacAddress] =
                    (GetMetricFamily(reading.Metric), key);
            }
            else
            {
                _recentCriticalReadings.Remove(
                    reading.SensorMacAddress);
            }
        }
    }

    private void UpdateInsight(Telemetry reading)
    {
        _suggestions.RemoveAll(
            s => s.SensorMacAddress.Equals(
                reading.SensorMacAddress,
                StringComparison.OrdinalIgnoreCase));

        var context = GetLearningContext(
            reading.SensorMacAddress,
            reading.Metric);

        var pattern = _commandPatterns
            .Where(pair => pair.Key.StartsWith(
                context + "|",
                StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .OrderByDescending(value => value.Count)
            .FirstOrDefault();

        if (
            pattern.Count >= MinimumLearningEvidence &&
            IsCritical(reading))
        {
            double confidence =
                pattern.Count / (double)(pattern.Count + 2);

            if (confidence >= MinimumLearningConfidence)
            {
                _suggestions.Add(new SuggestedAction
                {
                    SensorMacAddress = reading.SensorMacAddress,

                    Title = $"Context-based suggestion: {pattern.Command}",

                    Reason =
                        $"This device category and metric context has " +
                        $"{pattern.Count} matching operator examples. " +
                        "Treat this as advisory evidence, not an automatic control decision.",

                    SuggestedCommand = pattern.Command,
                    SuggestedValue = pattern.Value,
                    Priority = "High",
                    BasedOnHistory = true,
                    EvidenceCount = pattern.Count,
                    Confidence = confidence
                });

                return;
            }
        }

        if (
            reading.Metric.Contains(
                "moisture",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value < 20)
        {
            _suggestions.Add(new SuggestedAction
            {
                SensorMacAddress = reading.SensorMacAddress,
                Title = "Check irrigation",
                Reason =
                    $"Moisture is low ({reading.Value}). Inspect the sensor and consider activating the water pump.",
                SuggestedCommand = "Water Pump",
                SuggestedValue = "ON",
                Priority = "Critical"
            });
        }
        else if (
            reading.Metric.Contains(
                "power",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value > 1500)
        {
            _suggestions.Add(new SuggestedAction
            {
                SensorMacAddress = reading.SensorMacAddress,
                Title = "Investigate power spike",
                Reason =
                    $"Power reading is high ({reading.Value}). Check the connected load before overriding a relay.",
                SuggestedCommand = "Smart Relay",
                SuggestedValue = "OFF",
                Priority = "High"
            });
        }
        else if (
            reading.Metric.Contains(
                "temperature",
                StringComparison.OrdinalIgnoreCase) &&
            (reading.Value < 15 || reading.Value > 35))
        {
            _suggestions.Add(new SuggestedAction
            {
                SensorMacAddress = reading.SensorMacAddress,
                Title = "Inspect temperature sensor",
                Reason =
                    $"Temperature reading ({reading.Value}) is outside the configured range.",
                SuggestedCommand = "Request Sensor Check",
                SuggestedValue = "RUN",
                Priority = "High"
            });
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

            if (
                _recentCriticalReadings.TryGetValue(
                    request.SensorMacAddress,
                    out var recent) &&
                DateTime.UtcNow - recent.Timestamp <=
                    TimeSpan.FromMinutes(30))
            {
                var context = GetLearningContext(
                    request.SensorMacAddress,
                    recent.Metric);

                var patternKey =
                    $"{context}|{request.Command}|{request.Value}";

                if (_commandPatterns.TryGetValue(
                    patternKey,
                    out var pattern))
                {
                    _commandPatterns[patternKey] =
                        (request.Command, request.Value, pattern.Count + 1);
                }
                else
                {
                    _commandPatterns[patternKey] =
                        (request.Command, request.Value, 1);
                }
            }

            return command;
        }
    }

    public DeviceCommand? UndoLastCommand()
    {
        lock (_sync)
        {
            if (!_commandStack.TryPop(out var last))
                return null;

            var undo = new DeviceCommand
            {
                SensorMacAddress = last.SensorMacAddress,

                Command = $"UNDO: {last.Command}",

                Value = last.Value == "ON"
                    ? "OFF"
                    : last.Value == "OFF"
                        ? "ON"
                        : "REVERT",

                CreatedAtUtc = DateTime.UtcNow,

                Status = "Simulated undo"
            };

            _commands.Add(undo);

            return undo;
        }
    }

    /// <summary>
    /// Gets the newest history readings, ordered newest first.
    /// </summary>
    public List<Telemetry> GetHistory(int count = 100)
    {
        lock (_sync)
        {
            return _history.GetRange(
                    DateTime.SpecifyKind(
                        DateTime.MinValue,
                        DateTimeKind.Utc),
                    DateTime.SpecifyKind(
                        DateTime.MaxValue,
                        DateTimeKind.Utc))
                .OrderByDescending(t => t.Timestamp)
                .Take(Math.Clamp(count, 1, 500))
                .ToList();
        }
    }

    /// <summary>
    /// Gets readings for one sensor and metric inside a timestamp range.
    /// </summary>
    public List<Telemetry> GetHistoryRange(
        string sensorMacAddress,
        string metric,
        DateTime fromUtc,
        DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            throw new ArgumentException(
                "The start of the range must be earlier than or equal to its end.");
        }

        lock (_sync)
        {
            return _history.GetRange(fromUtc, toUtc)
                .Where(item =>
                    item.SensorMacAddress.Equals(
                        sensorMacAddress,
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Metric.Equals(
                        metric,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Timestamp)
                .ToList();
        }
    }

    public List<SuggestedAction> GetSuggestions()
    {
        lock (_sync)
        {
            return _suggestions.ToList();
        }
    }

    private string GetLearningContext(
        string mac,
        string metric)
    {
        var category = _devices.TryGetValue(mac, out var foundCategory)
            ? foundCategory
            : "unknown";

        return
            $"{category.Trim().ToLowerInvariant()}|{GetMetricFamily(metric)}";
    }

    private static bool IsCritical(Telemetry reading)
    {
        return TryGetNormalisedSeverity(reading, out _);
    }

    /// <summary>
    /// Calculates severity as a proportional distance beyond
    /// each metric's own threshold.
    /// </summary>
    private static bool TryGetNormalisedSeverity(
        Telemetry reading,
        out double severity)
    {
        severity = 0;

        var metric = reading.Metric;

        if (
            metric.Contains(
                "moisture",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value < 20)
        {
            severity = (20 - reading.Value) / 20.0;
        }
        else if (
            metric.Contains(
                "power",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value > 1500)
        {
            severity = (reading.Value - 1500) / 1500.0;
        }
        else if (
            metric.Contains(
                "temperature",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value < 15)
        {
            severity = (15 - reading.Value) / 15.0;
        }
        else if (
            metric.Contains(
                "temperature",
                StringComparison.OrdinalIgnoreCase) &&
            reading.Value > 35)
        {
            severity = (reading.Value - 35) / 35.0;
        }

        return severity > 0 && double.IsFinite(severity);
    }

    private static string GetMetricFamily(string metric)
    {
        if (metric.Contains(
            "moisture",
            StringComparison.OrdinalIgnoreCase))
            return "moisture";

        if (metric.Contains(
            "power",
            StringComparison.OrdinalIgnoreCase))
            return "power";

        if (metric.Contains(
            "temperature",
            StringComparison.OrdinalIgnoreCase))
            return "temperature";

        return metric.Trim().ToLowerInvariant();
    }
}

public sealed record ProcessedTelemetry(
    string SensorMacAddress,
    string Metric,
    double Value,
    string Queue);

public sealed record QueueProcessingResult(
    int CriticalProcessed,
    int FifoProcessed,
    IReadOnlyList<ProcessedTelemetry> ProcessingOrder);
