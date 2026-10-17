# Smart-X IoT Mesh Ecosystem — Part 2

A beginner-friendly simulated IoT monitoring and command application built with **.NET 10 Web API**, **Blazor WebAssembly**, and **Azure Table Storage**. Local development uses Azurite for tables and a local-folder substitute for Azure Files as azure subscripion has expired,proof is shown below. No physical sensors are required. Commands are simulations and do not control real equipment.

## Features

- Register and manage sensors (MAC address, location and category).
- Generate and inspect mock temperature, power and valve telemetry.
- View sensor health, historical readings and detected anomalies.
- Process readings through a normal FIFO queue and a critical priority queue.
- Use a `Dictionary` for fast device lookup, a `SortedDictionary` for timestamp history, a `HashSet` for unique active alerts, and a `Stack` for command Undo.
- Issue simulated pump, valve and relay commands.
- Filter devices by category or active alert state.
- Show automated suggested actions based on critical readings.
- API health endpoint and configuration-based client API URL.
- Landing page shows API status and all three assignment pillars. Network Topology is disabled for Part 3.

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
| GET | `/api/command-stream/status` | Queue and processor status |
| POST | `/api/command-stream/process` | Process pending critical and FIFO telemetry queues |
| POST | `/api/command-stream/simulate/{macAddress}?count=100` | Process existing readings through Part 2 structures |
| POST | `/api/command-stream/commands` | Record a simulated device command |
| POST | `/api/command-stream/undo` | Undo the last recorded command |
| GET | `/api/command-stream/history?count=100` | Get processed telemetry history |
| GET | `/api/command-stream/suggestions` | Get automated suggested actions |
| POST | `/api/documents/upload` | Upload a document (if configured) |
| GET | `/api/documents` | List documents (if configured) |
| GET | `/api/documents/download/{fileName}` | Download a document (if configured) |

## Part 2 data structures

- **Queue:** ordinary telemetry waits in FIFO order until the operator processes the queue.
- **PriorityQueue:** critical temperature, power or moisture readings wait separately and are drained before normal FIFO readings; more extreme critical readings are processed first.
- **Stack:** manual commands can be undone in last-in, first-out order.
- **Dictionary:** caches registered device categories for O(1) telemetry-ingestion validation and lookup.
- **SortedDictionary:** groups telemetry by timestamp for ordered history.
- **HashSet:** keeps active alert identifiers unique so duplicate alert keys are not added.
- **Recommendation engine:** evaluates processed readings and suggests an investigation or simulated action for low moisture, high power or out-of-range temperature. It also learns command/metric associations when an operator issues a command after a critical reading and surfaces the learned command on a later matching critical reading.

Use **Process Incoming Queue** on the command-stream page to demonstrate critical-priority processing before normal FIFO readings. `/api/command-stream/process` returns the processing order and counts. These processing structures and learned command patterns are in-memory demonstration structures. Azure Tables remain the persistent store for sensor and telemetry records. A restart clears the in-memory Part 2 queues, command history and learned patterns.

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
