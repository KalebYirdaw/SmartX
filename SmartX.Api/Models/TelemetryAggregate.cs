namespace SmartX.Api.Models
{
    /// <summary>
    /// Represents an aggregated telemetry measurement.
    /// Operator overloading allows multiple measurements
    /// to be combined using the + operator.
    /// </summary>
    public class TelemetryAggregate
    {
        public string Metric { get; set; } = string.Empty;

        public double Value { get; set; }

        public int SensorCount { get; set; }

        public TelemetryAggregate()
        {
        }

        public TelemetryAggregate(
            string metric,
            double value,
            int sensorCount)
        {
            Metric = metric;
            Value = value;
            SensorCount = sensorCount;
        }

        /// <summary>
        /// Combines two telemetry aggregates.
        /// </summary>
        public static TelemetryAggregate operator +(
            TelemetryAggregate first,
            TelemetryAggregate second)
        {
            if (!string.Equals(
                    first.Metric,
                    second.Metric,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Telemetry metrics must match before they can be aggregated.");
            }

            return new TelemetryAggregate(
                first.Metric,
                first.Value + second.Value,
                first.SensorCount + second.SensorCount);
        }
    }
}