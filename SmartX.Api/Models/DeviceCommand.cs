namespace SmartX.Api.Models;

public class DeviceCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SensorMacAddress { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Simulated";
}

public class CommandRequest
{
    public string SensorMacAddress { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class SuggestedAction
{
    public string SensorMacAddress { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string SuggestedCommand { get; set; } = string.Empty;
    public string SuggestedValue { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public bool BasedOnHistory { get; set; }
    public int EvidenceCount { get; set; }
}
