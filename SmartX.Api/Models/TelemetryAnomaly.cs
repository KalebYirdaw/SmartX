namespace SmartX.Api.Models
{
    /// <summary>
    /// Represents a telemetry reading that has been
    /// identified as anomalous.
    /// </summary>
    public class TelemetryAnomaly
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}