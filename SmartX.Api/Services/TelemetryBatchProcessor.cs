using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Processes telemetry batches using a jagged array
    /// before converting the readings into a List.
    /// </summary>
    public class TelemetryBatchProcessor
    {
        /// <summary>
        /// Converts telemetry batches into a single list.
        /// Each inner array represents one incoming telemetry batch.
        /// </summary>
        public List<Telemetry> FlattenBatches(
            Telemetry[][] telemetryBatches)
        {
            var telemetryList = new List<Telemetry>();

            for (int batchIndex = 0;
                 batchIndex < telemetryBatches.Length;
                 batchIndex++)
            {
                var batch = telemetryBatches[batchIndex];

                for (int readingIndex = 0;
                     readingIndex < batch.Length;
                     readingIndex++)
                {
                    telemetryList.Add(
                        batch[readingIndex]);
                }
            }

            return telemetryList;
        }
    }
}