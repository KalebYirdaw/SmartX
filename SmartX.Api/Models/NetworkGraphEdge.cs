namespace SmartX.Api.Models;

/// <summary>
/// Represents a wireless connection between two network devices.
/// The connection is treated as undirected.
/// </summary>
public sealed class NetworkGraphEdge
{
    public string SourceMacAddress { get; set; } = string.Empty;

    public string TargetMacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Positive connection cost. Lower values represent better routes.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Optional wireless signal strength, normally represented in dBm.
    /// </summary>
    public double SignalStrength { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
