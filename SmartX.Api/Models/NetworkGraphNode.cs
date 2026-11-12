namespace SmartX.Api.Models;

/// <summary>
/// Represents a sensor or device in the wireless mesh network.
/// </summary>
public sealed class NetworkGraphNode
{
    public string MacAddress { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool IsOnline { get; set; } = true;

    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}
