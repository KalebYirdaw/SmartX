namespace SmartX.Api.Models
{
    /// <summary>
    /// Represents the current connectivity and health
    /// status of a Smart-X sensor.
    /// </summary>
    public class SensorHealth
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? LastTelemetryTime { get; set; }

        public double? MinutesSinceLastTelemetry { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}