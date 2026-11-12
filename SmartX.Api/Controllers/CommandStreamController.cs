using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/command-stream")]
public class CommandStreamController : ControllerBase
{
    private readonly CommandStreamService _stream;
    private readonly SensorTableService _sensors;
    private readonly TelemetryService _telemetry;

    public CommandStreamController(CommandStreamService stream, SensorTableService sensors, TelemetryService telemetry)
    {
        _stream = stream;
        _sensors = sensors;
        _telemetry = telemetry;
    }

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(_stream.Snapshot());

    [HttpPost("process")]
    public IActionResult ProcessQueues([FromQuery] int maxItems = 100)
    {
        if (maxItems < 1 || maxItems > 1_000)
            return BadRequest(new { message = "maxItems must be between 1 and 1,000." });
        return Ok(_stream.ProcessQueuedTelemetry(maxItems));
    }

    [HttpPost("commands")]
    public async Task<IActionResult> IssueCommand([FromBody] CommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SensorMacAddress) ||
            string.IsNullOrWhiteSpace(request.Command) ||
            string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { message = "Sensor, command and value are required." });

        var sensor = await _sensors.GetSensorAsync(request.SensorMacAddress);
        if (sensor is null) return NotFound(new { message = "Register this sensor before sending a command." });

        if (!new[] { "Water Pump", "Smart Relay", "Valve", "Request Sensor Check" }.Contains(request.Command))
            return BadRequest(new { message = "Unsupported command." });

        if (!new[] { "ON", "OFF", "OPEN", "CLOSED", "RUN" }.Contains(request.Value.ToUpperInvariant()))
            return BadRequest(new { message = "Value must be ON, OFF, OPEN, CLOSED or RUN." });

        request.Value = request.Value.ToUpperInvariant();
        var command = _stream.IssueCommand(request);
        return Ok(new { message = "Command recorded as a simulation; no physical device was contacted.", command });
    }

    [HttpPost("undo")]
    public IActionResult Undo()
    {
        var command = _stream.UndoLastCommand();
        return command is null ? NotFound(new { message = "There are no commands to undo." }) : Ok(command);
    }

    [HttpGet("history")]
    public IActionResult History([FromQuery] int count = 100) => Ok(_stream.GetHistory(count));

    [HttpGet("history/range")]
    public IActionResult HistoryRange(
        [FromQuery] string sensorMacAddress,
        [FromQuery] string metric,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc)
    {
        if (string.IsNullOrWhiteSpace(sensorMacAddress) || string.IsNullOrWhiteSpace(metric))
            return BadRequest(new { message = "sensorMacAddress and metric are required." });
        if (fromUtc > toUtc)
            return BadRequest(new { message = "fromUtc must be earlier than or equal to toUtc." });

        return Ok(_stream.GetHistoryRange(sensorMacAddress, metric, fromUtc, toUtc));
    }

    [HttpGet("suggestions")]
    public IActionResult Suggestions() => Ok(_stream.GetSuggestions());

    [HttpPost("simulate/{macAddress}")]
    public async Task<IActionResult> Simulate(string macAddress, [FromQuery] int count = 20)
    {
        var sensor = await _sensors.GetSensorAsync(macAddress);
        if (sensor is null) return NotFound(new { message = "Sensor not found. Register it first." });
        if (count < 1 || count > 500) return BadRequest(new { message = "Count must be between 1 and 500." });

        var readings = await _telemetry.GetTelemetryAsync(macAddress);
        var latest = readings.OrderByDescending(x => x.Timestamp).Take(count).OrderBy(x => x.Timestamp).ToList();
        foreach (var reading in latest) _stream.EnqueueTelemetry(reading);
        var result = _stream.ProcessQueuedTelemetry();
        return Ok(new
        {
            message = "Existing telemetry passed through the command-stream processor.",
            enqueued = latest.Count,
            criticalProcessed = result.CriticalProcessed,
            fifoProcessed = result.FifoProcessed,
            processingOrder = result.ProcessingOrder,
            status = _stream.Snapshot()
        });
    }
}
