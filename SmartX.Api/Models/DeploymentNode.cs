namespace SmartX.Api.Models
{
    /// <summary>
    /// Represents a hierarchical Smart-X deployment structure.
    /// A node can contain child facilities, zones, sub-zones or sensors.
    /// </summary>
    public class DeploymentNode
    {
        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string SensorMacAddress { get; set; } = string.Empty;

        public List<DeploymentNode> Children { get; set; } = new();
    }
}
