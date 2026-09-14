using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Determines whether a sensor is online, stale,
    /// or disconnected based on its latest telemetry.
    /// </summary>
    public class SensorHealthService
    {
        private readonly TelemetryService _telemetryService;

        public SensorHealthService(
            TelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        public async Task<SensorHealth> GetSensorHealthAsync(
            string sensorMacAddress)
        {
            if (string.IsNullOrWhiteSpace(sensorMacAddress))
            {
                throw new ArgumentException(
                    "Sensor MAC address is required.");
            }

            var telemetry =
                await _telemetryService.GetTelemetryAsync(
                    sensorMacAddress);

            if (telemetry.Count == 0)
            {
                return new SensorHealth
                {
                    SensorMacAddress = sensorMacAddress,
                    Status = "Disconnected",
                    LastTelemetryTime = null,
                    MinutesSinceLastTelemetry = null,
                    Message =
                        "No telemetry has been received from this sensor."
                };
            }

            var latestTelemetry =
                telemetry
                    .OrderByDescending(t => t.Timestamp)
                    .First();

            var lastTelemetryTime =
                latestTelemetry.Timestamp.ToUniversalTime();

            var minutesSinceLastTelemetry =
                (DateTime.UtcNow - lastTelemetryTime)
                .TotalMinutes;

            string status;
            string message;

            if (minutesSinceLastTelemetry <= 5)
            {
                status = "Online";
                message =
                    "Sensor is reporting normally.";
            }
            else if (minutesSinceLastTelemetry <= 15)
            {
                status = "Stale";
                message =
                    "Sensor telemetry has not been received recently.";
            }
            else
            {
                status = "Disconnected";
                message =
                    "Sensor has not reported telemetry for more than 15 minutes.";
            }

            return new SensorHealth
            {
                SensorMacAddress = sensorMacAddress,
                Status = status,
                LastTelemetryTime = lastTelemetryTime,
                MinutesSinceLastTelemetry =
                    Math.Round(
                        minutesSinceLastTelemetry,
                        2),
                Message = message
            };
        }
    }
}