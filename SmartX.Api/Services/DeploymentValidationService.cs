using SmartX.Api.Models;

namespace SmartX.Api.Services
{
    /// <summary>
    /// Recursively validates the Smart-X deployment hierarchy.
    /// </summary>
    public class DeploymentValidationService
    {
        public List<string> ValidateDeployment(
            DeploymentNode node)
        {
            var errors = new List<string>();

            ValidateNode(node, errors, "Root");

            return errors;
        }

        private void ValidateNode(
            DeploymentNode node,
            List<string> errors,
            string path)
        {
            if (string.IsNullOrWhiteSpace(node.Name))
            {
                errors.Add(
                    $"{path}: deployment node name is required.");
            }

            if (string.IsNullOrWhiteSpace(node.Type))
            {
                errors.Add(
                    $"{path}: deployment node type is required.");
            }

            if (node.Type.Equals(
                    "Sensor",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(
                        node.SensorMacAddress))
                {
                    errors.Add(
                        $"{path}: sensor MAC address is required.");
                }
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                var childPath =
                    $"{path} -> {node.Children[i].Name}";

                // Recursive call
                ValidateNode(
                    node.Children[i],
                    errors,
                    childPath);
            }
        }
    }
}