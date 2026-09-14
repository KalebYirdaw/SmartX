using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Processes strongly typed telemetry packets and converts
    /// them into the common telemetry storage model.
    /// </summary>
    public class TelemetryPacketProcessor
    {
        public Telemetry ConvertToTelemetry<T>(
            TelemetryPacket<T> packet)
        {
            if (string.IsNullOrWhiteSpace(packet.SensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

            if (string.IsNullOrWhiteSpace(packet.Metric))
            {
                throw new ArgumentException(
                    "Telemetry metric is required.");
            }

            return new Telemetry
            {
                SensorMacAddress = packet.SensorMacAddress,
                Timestamp = packet.Timestamp == default
                    ? DateTime.UtcNow
                    : packet.Timestamp,
                Metric = packet.Metric,
                Value = Convert.ToDouble(packet.Value)
            };
        }
    }
}