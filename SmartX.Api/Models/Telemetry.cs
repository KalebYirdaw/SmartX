namespace SmartX.Api.Models
{
    public class Telemetry
    {
        public string SensorMacAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }
    }
}