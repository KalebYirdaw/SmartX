using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Detects unusual telemetry readings using
    /// metric-specific threshold rules.
    /// </summary>
    public class TelemetryAnomalyService
    {
        public List<TelemetryAnomaly> DetectAnomalies(
            IEnumerable<Telemetry> telemetry)
        {
            var anomalies = new List<TelemetryAnomaly>();

            foreach (var reading in telemetry)
            {
                if (IsTemperatureAnomaly(reading))
                {
                    anomalies.Add(
                        CreateAnomaly(
                            reading,
                            "Temperature outside normal operating range."));
                }
                else if (IsPowerAnomaly(reading))
                {
                    anomalies.Add(
                        CreateAnomaly(
                            reading,
                            "Power consumption is unusually high."));
                }
            }

            return anomalies
                .OrderByDescending(a => a.Timestamp)
                .ToList();
        }

        private static bool IsTemperatureAnomaly(
            Telemetry reading)
        {
            return reading.Metric.Equals(
                       "Temperature",
                       StringComparison.OrdinalIgnoreCase)
                   &&
                   (reading.Value < 15 ||
                    reading.Value > 35);
        }

        private static bool IsPowerAnomaly(
            Telemetry reading)
        {
            return reading.Metric.Equals(
                       "Power",
                       StringComparison.OrdinalIgnoreCase)
                   &&
                   reading.Value > 1500;
        }

        private static TelemetryAnomaly CreateAnomaly(
            Telemetry reading,
            string reason)
        {
            return new TelemetryAnomaly
            {
                SensorMacAddress =
                    reading.SensorMacAddress,

                Timestamp =
                    reading.Timestamp,

                Metric =
                    reading.Metric,

                Value =
                    reading.Value,

                Reason =
                    reason
            };
        }
    }
}