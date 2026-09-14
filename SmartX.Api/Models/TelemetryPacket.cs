namespace SmartX.Api.Models
{
    /// <summary>
    /// Generic telemetry packet capable of carrying different
    /// telemetry value types such as float, int and bool.
    /// </summary>
    public class TelemetryPacket<T>
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public T Value { get; set; } = default!;
    }
}