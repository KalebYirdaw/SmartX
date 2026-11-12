using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Controllers;

[ApiController]
[Route("api/network-topology")]
public class NetworkTopologyController : ControllerBase
{
    private readonly NetworkTopologyService _topologyService;
    private readonly SensorTableService _sensorTableService;

    public NetworkTopologyController(
        NetworkTopologyService topologyService,
        SensorTableService sensorTableService)
    {
        _topologyService = topologyService;
        _sensorTableService = sensorTableService;
    }

    // GET: api/network-topology
    [HttpGet]
    public async Task<IActionResult> GetTopology()
    {
        await SyncSensorsAsync();

        return Ok(_topologyService.GetTopology());
    }

    // GET: api/network-topology/nodes
    [HttpGet("nodes")]
    public async Task<IActionResult> GetNodes()
    {
        await SyncSensorsAsync();

        return Ok(_topologyService.GetDevices());
    }

    // GET: api/network-topology/edges
    [HttpGet("edges")]
    public IActionResult GetEdges()
    {
        return Ok(_topologyService.GetConnections());
    }

    // GET: api/network-topology/nodes/{macAddress}/neighbours
    [HttpGet("nodes/{macAddress}/neighbours")]
    public IActionResult GetNeighbours(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return BadRequest(new { message = "MAC address is required." });

        var device = _topologyService.GetDevice(macAddress);

        if (device is null)
            return NotFound(new { message = "Device not found." });

        return Ok(_topologyService.GetNeighbours(macAddress));
    }

    // GET: api/network-topology/bfs/{macAddress}
    [HttpGet("bfs/{macAddress}")]
    public async Task<IActionResult> RunBreadthFirstSearch(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return BadRequest(new
            {
                message = "Starting device MAC address is required."
            });
        }

        await SyncSensorsAsync();

        try
        {
            return Ok(_topologyService.TraverseBreadthFirst(macAddress));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    // GET: api/network-topology/dfs/{macAddress}
    [HttpGet("dfs/{macAddress}")]
    public async Task<IActionResult> RunDepthFirstSearch(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return BadRequest(new
            {
                message = "Starting device MAC address is required."
            });
        }

        await SyncSensorsAsync();

        try
        {
            return Ok(_topologyService.TraverseDepthFirst(macAddress));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    // GET: api/network-topology/mst/prim
    [HttpGet("mst/prim")]
    public async Task<IActionResult> CalculatePrimMst()
    {
        await SyncSensorsAsync();

        return Ok(
            _topologyService.CalculatePrimMinimumSpanningTree());
    }

    // GET: api/network-topology/mst/kruskal
    [HttpGet("mst/kruskal")]
    public async Task<IActionResult> CalculateKruskalMst()
    {
        await SyncSensorsAsync();

        return Ok(
            _topologyService.CalculateKruskalMinimumSpanningTree());
    }

    // POST: api/network-topology/edges
    [HttpPost("edges")]
    public IActionResult AddConnection(
        [FromBody] NetworkGraphEdge edge)
    {
        try
        {
            var added = _topologyService.AddConnection(edge);

            if (!added)
            {
                return NotFound(new
                {
                    message = "Both devices must be registered before connecting them."
                });
            }

            return Ok(new
            {
                message = "Wireless connection added successfully.",
                edge
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // DELETE: api/network-topology/edges/{sourceMac}/{targetMac}
    [HttpDelete("edges/{sourceMac}/{targetMac}")]
    public IActionResult RemoveConnection(
        string sourceMac,
        string targetMac)
    {
        var removed = _topologyService.RemoveConnection(
            sourceMac,
            targetMac);

        if (!removed)
        {
            return NotFound(new
            {
                message = "Wireless connection not found."
            });
        }

        return Ok(new
        {
            message = "Wireless connection removed successfully."
        });
    }

    // PUT: api/network-topology/nodes/{macAddress}/status
    [HttpPut("nodes/{macAddress}/status")]
    public IActionResult UpdateDeviceStatus(
        string macAddress,
        [FromBody] DeviceStatusRequest request)
    {
        var updated = _topologyService.UpdateDeviceStatus(
            macAddress,
            request.IsOnline);

        if (!updated)
        {
            return NotFound(new
            {
                message = "Device not found."
            });
        }

        return Ok(new
        {
            message = "Device status updated successfully.",
            macAddress,
            request.IsOnline
        });
    }

    private async Task SyncSensorsAsync()
    {
        var sensors = await _sensorTableService.GetSensorsAsync();

        foreach (var sensor in sensors)
        {
            _topologyService.RegisterSensor(sensor);
        }
    }
}

public sealed class DeviceStatusRequest
{
    public bool IsOnline { get; set; }
}
