# Smart-X IoT Mesh Ecosystem — Part 2

A beginner-friendly simulated IoT monitoring and command application built with **.NET 10 Web API**, **Blazor WebAssembly**, and **Azure Table Storage**. Local development uses Azurite for tables and a local-folder substitute for Azure Files as azure subscripion has expired,proof is shown below. No physical sensors are required. Commands are simulations and do not control real equipment.

## Features

- Register and manage sensors (MAC address, location and category).
- Generate and inspect mock temperature, power and valve telemetry.
- View sensor health, historical readings and detected anomalies.
- Process readings automatically in bounded batches through a normal FIFO queue and a normalised-severity critical priority queue.
- Use a `Dictionary` for fast device lookup, a `SortedDictionary` for timestamp history, a `HashSet` for unique active alerts, and a `Stack` for command Undo.
- Issue simulated pump, valve and relay commands.
- Filter devices by category or active alert state.
- Show automated suggested actions based on critical readings, with context-specific learning, minimum evidence and confidence.
- API health endpoint and configuration-based client API URL.
- Landing page shows API status and all three assignment pillars; network reachability analysis is available through the topology API.
- Analyze network reachability over active wireless connections, traversing only through online devices and reporting unreachable devices with their online status.

## Projects

```text
SmartX-main/
├── SmartX.Api/       # .NET 10 backend API
└── SmartX.Client/    # Blazor WebAssembly frontend
```

## Requirements

- .NET 10 SDK
- Visual Studio or another .NET IDE
- Azurite for local Azure Table Storage
- A local folder for file uploads in Development (Azurite does not emulate Azure Files)
- Optional: Postman

## Run locally

Run the commands below from the `SmartX-main` folder. Keep each service running in its own terminal.

### 1. Start Azurite

Install Azurite once if it is not already installed:

```powershell
npm.cmd install --global azurite
```

In the first terminal, start Azurite. Resolving its npm install location this way also works when the npm global folder is not on `PATH`:

```powershell
$npmPrefix = (npm.cmd config get prefix).Trim()
& (Join-Path $npmPrefix "azurite.cmd") --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0
```

### 2. Configure storage

Development is configured to use Azurite for Azure Tables and a local folder (`SmartX.Api/App_Data/sensor-files`) for document and sensor-file uploads. This works without an Azure account or storage charges. Uploaded local files are ignored by Git.

The local folder keeps the file endpoints usable for demonstrations, but it is **not Azure Files**. If the assessment specifically requires a real Azure File Share, confirm with your assessor whether the local substitute is acceptable; the project keeps an Azure Files provider for use with an active Azure Storage Account.

For Azure deployment, set `FileStorage:Provider` to `Azure` and supply both `ConnectionStrings:AzureStorage` and `ConnectionStrings:AzureFileStorage` using user secrets or environment variables. Never commit production connection strings or secrets to source control.

### 3. Start the API

In a second terminal:

```powershell
Set-Location .\SmartX.Api
dotnet restore
dotnet run
```

The development API is configured for `http://localhost:5212`. Health check:

```http
GET http://localhost:5212/api/health
```

The response should contain `"status": "Healthy"`.

### 4. Start the client

In a third terminal:

```powershell
Set-Location .\SmartX.Client
dotnet restore
dotnet run
```

The client is configured for `http://localhost:5221`. The API address is read from `SmartX.Client/wwwroot/appsettings.json` (`ApiBaseUrl`) instead of being hard-coded in `Program.cs`. Change this setting if your API runs at another address.

## Useful endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/health` | API health status |
| GET | `/api/Sensor` | List registered sensors |
| POST | `/api/Sensor` | Register a sensor |
| PUT | `/api/Sensor/{macAddress}` | Update a sensor |
| DELETE | `/api/Sensor/{macAddress}` | Delete a sensor |
| POST | `/api/Telemetry` | Submit telemetry for a registered sensor |
| GET | `/api/Telemetry/{sensorMacAddress}` | Read telemetry history |
| POST | `/api/Telemetry/seed/{sensorMacAddress}?count=100` | Generate mock readings |
| GET | `/api/Telemetry/anomalies/{sensorMacAddress}` | Get detected anomalies |
| GET | `/api/SensorHealth/{sensorMacAddress}` | Get sensor health |
| GET | `/api/network-topology` | Get the current network topology |
| GET | `/api/network-topology/nodes` | List registered network devices |
| GET | `/api/network-topology/edges` | List wireless connections |
| GET | `/api/network-topology/nodes/{macAddress}/neighbours` | List a device's connections |
| GET | `/api/network-topology/bfs/{macAddress}` | Traverse active online connections breadth-first and report unreachable devices |
| GET | `/api/network-topology/dfs/{macAddress}` | Traverse active online connections depth-first and report unreachable devices |
| GET | `/api/network-topology/mst/prim` | Calculate minimum spanning forest using Prim's algorithm |
| GET | `/api/network-topology/mst/kruskal` | Calculate minimum spanning forest using Kruskal's algorithm |
| POST | `/api/network-topology/edges` | Add a wireless connection |
| DELETE | `/api/network-topology/edges/{sourceMac}/{targetMac}` | Remove a wireless connection |
| PUT | `/api/network-topology/nodes/{macAddress}/status` | Update a device's online status |
| GET | `/api/command-stream/status` | Queue and processor status |
| POST | `/api/command-stream/process` | Process pending critical and FIFO telemetry queues |
| POST | `/api/command-stream/simulate/{macAddress}?count=100` | Process existing readings through Part 2 structures |
| POST | `/api/command-stream/commands` | Record a simulated device command |
| POST | `/api/command-stream/undo` | Undo the last recorded command |
| GET | `/api/command-stream/history?count=100` | Get processed telemetry history |
| GET | `/api/command-stream/history/range?sensorMacAddress=AA:BB:CC:DD:EE:01&metric=moisture&fromUtc=2026-11-12T10:00:00Z&toUtc=2026-11-12T12:00:00Z` | Query one sensor and metric in an inclusive UTC time range |
| GET | `/api/command-stream/suggestions` | Get automated suggested actions |
| POST | `/api/documents/upload` | Upload a document (if configured) |
| GET | `/api/documents` | List documents (if configured) |
| GET | `/api/documents/download/{fileName}` | Download a document (if configured) |

### Minimum Spanning Forest Algorithms

Smart-X supports two minimum spanning forest algorithms:

- **Prim's Algorithm:** Builds a minimum spanning forest by selecting the lowest-weight active connection from the current network component.
- **Kruskal's Algorithm:** Sorts active connections by weight and uses Disjoint Set Union to prevent cycles.

Both algorithms:

- Consider online devices only and include isolated online devices in the component count.
- Ignore offline devices and inactive wireless connections.
- Avoid cycles while supporting disconnected network components.
- Return selected connections, total connection weight, component count, and connectivity status.

#### API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/network-topology/mst/prim` | Calculate minimum spanning forest using Prim's algorithm |
| GET | `/api/network-topology/mst/kruskal` | Calculate minimum spanning forest using Kruskal's algorithm |

## Part 2 processing improvements

- A hosted background worker checks the incoming queues every second and processes up to 100 readings per tick; the API manual process endpoint accepts a bounded batch size (`maxItems=1..1000`).
- Critical severity is dimensionless: it is calculated as the proportional distance beyond the threshold for that metric. Moisture percentages, power wattage and temperature are therefore not directly compared as raw numbers.
- The timestamp-sorted in-memory history is capped at 50,000 readings, removing the oldest timestamps first.
- The range endpoint filters by sensor MAC address, metric and inclusive UTC start/end timestamps.
- The command-stream page refreshes its status and suggestions every three seconds and cancels its refresh loop when the component is disposed.
- Learned suggestions are scoped by registered device category and metric family. A suggestion requires at least three matching operator examples and at least 60% confidence; the confidence is displayed as a percentage.
- Automatic queue processing is incremental. It does not wait for the user to press the manual processing button, and a large backlog is spread across multiple worker ticks.

## Part 2 data structures

- **Queue:** ordinary telemetry waits in FIFO order until the operator processes the queue.
- **PriorityQueue:** critical temperature, power or moisture readings wait separately and are drained before normal FIFO readings; the highest normalised deviation is processed first.
- **Stack:** manual commands can be undone in last-in, first-out order.
- **Dictionary:** caches registered device categories for O(1) telemetry-ingestion validation and lookup.
- **SortedDictionary:** groups telemetry by timestamp for ordered history.
- **HashSet:** keeps active alert identifiers unique so duplicate alert keys are not added.
- **Recommendation engine:** evaluates processed readings and suggests an investigation or simulated action for low moisture, high power or out-of-range temperature. It learns only within the same registered device category and metric family, and only surfaces learned suggestions after at least three examples and a confidence score of 60% or higher.

The background worker normally processes queued readings automatically in batches of up to 100. Use **Process Incoming Queue** on the command-stream page to manually process a bounded batch and demonstrate critical-priority processing before normal FIFO readings. `/api/command-stream/process` returns the processing order and counts. These processing structures and learned command patterns are in-memory demonstration structures. Azure Tables remain the persistent store for sensor and telemetry records. A restart clears the in-memory Part 2 queues, command history and learned patterns.

## Screenshot

<img width="1365" height="767" alt="Screenshot 2026-10-17 140716" src="https://github.com/user-attachments/assets/5ead6942-8c7e-4eb7-ba74-444232fa0cce" />
Azure account is expired so storage account was disabled hence I used local storage.

## Quick demonstration

1. Start Azurite, API and client.
2. Open **Sensor Management** and register a sensor, for example `AA:BB:CC:DD:EE:01`, location `Hydroponic Farm A`, category `Temperature`.
3. Open **Real-Time Command Stream**. Select the sensor and use **Send Test Reading** to post exact readings through the API.
4. Send `Power = 100`, `Moisture = 10`, and `Moisture = 5` without processing between them. The dashboard should show 1 FIFO and 2 critical readings waiting.
5. Choose **Process Incoming Queue**. The result reports `Moisture=5 (Critical)`, then `Moisture=10 (Critical)`, then `Power=100 (FIFO)`. Review the alert and Suggested Actions.
6. Issue **Water Pump → ON** after the critical moisture reading. Send and process another critical moisture reading to see the history-based suggestion with its evidence count.
7. Choose **Undo Last** to demonstrate the command stack.
8. Open the Home page to show API status and the three architectural pillars.

## YouTube link
- https://youtu.be/CkF14H82WP8

## Important notes

- Register a sensor before sending telemetry. Unregistered sensor telemetry is rejected.
- The command interface records simulated commands only; it does not control physical hardware.
- `Network Topology & Mesh Routing` is intentionally disabled until Part 3.
- co pilot was used to push latest projects to github 
