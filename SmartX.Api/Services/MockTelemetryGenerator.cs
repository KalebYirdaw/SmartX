using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Generates realistic mock telemetry for Smart-X testing
    /// and dashboard development.
    /// </summary>
    public class MockTelemetryGenerator
    {
        private readonly Random _random = new();

        public List<Telemetry> Generate(
            string sensorMacAddress,
            int readingCount = 100)
        {
            var readings = new List<Telemetry>();

            var startTime = DateTime.UtcNow
                .AddMinutes(-readingCount);

            for (int i = 0; i < readingCount; i++)
            {
                var timestamp =
                    startTime.AddMinutes(i);

                // Temperature represented as a float.
                var temperature =
                    22.0f +
                    (float)(_random.NextDouble() * 6.0);

                // Occasionally create an anomalous spike.
                if (i % 37 == 0)
                {
                    temperature += 12.0f;
                }

                var temperaturePacket =
                    new TelemetryPacket<float>
                    {
                        SensorMacAddress = sensorMacAddress,
                        Timestamp = timestamp,
                        Metric = "Temperature",
                        Value = temperature
                    };

                readings.Add(
                    ConvertPacket(temperaturePacket));

                // Power represented as an integer.
                var power =
                    _random.Next(400, 1200);

                var powerPacket =
                    new TelemetryPacket<int>
                    {
                        SensorMacAddress = sensorMacAddress,
                        Timestamp = timestamp,
                        Metric = "Power",
                        Value = power
                    };

                readings.Add(
                    ConvertPacket(powerPacket));

                // Valve state represented as a boolean.
                var valveOpen =
                    _random.Next(0, 2) == 1;

                var valvePacket =
                    new TelemetryPacket<bool>
                    {
                        SensorMacAddress = sensorMacAddress,
                        Timestamp = timestamp,
                        Metric = "ValveOpen",
                        Value = valveOpen
                    };

                readings.Add(
                    ConvertPacket(valvePacket));
            }

            return readings;
        }

        private static Telemetry ConvertPacket<T>(
            TelemetryPacket<T> packet)
        {
            return new Telemetry
            {
                SensorMacAddress =
                    packet.SensorMacAddress,

                Timestamp =
                    packet.Timestamp,

                Metric =
                    packet.Metric,

                Value =
                    Convert.ToDouble(packet.Value)
            };
        }
    }
}