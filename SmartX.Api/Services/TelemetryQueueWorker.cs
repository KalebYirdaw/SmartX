using SmartX.Api.Services;

namespace SmartX.Api.Services;

/// <summary>Continuously drains pending telemetry in bounded batches.</summary>
public sealed class TelemetryQueueWorker : BackgroundService
{
    private readonly CommandStreamService _commandStream;
    private readonly ILogger<TelemetryQueueWorker> _logger;

    public TelemetryQueueWorker(CommandStreamService commandStream, ILogger<TelemetryQueueWorker> logger)
    {
        _commandStream = commandStream;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var result = _commandStream.ProcessQueuedTelemetry(maxItems: 100);
                if (result.CriticalProcessed + result.FifoProcessed > 0)
                    _logger.LogInformation("Automatically processed {Critical} critical and {Fifo} FIFO telemetry readings.",
                        result.CriticalProcessed, result.FifoProcessed);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }
}
