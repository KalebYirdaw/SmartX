# Smart-X IoT Mesh Ecosystem -POE
## Network Topology, Mesh Routing and Telemetry Management

**Project:** Smart-X IoT Mesh Ecosystem  
**Technology:** C#, .NET 10, ASP.NET Core Web API, Blazor WebAssembly  
**Database / Storage:** Local development storage with Azure storage integration options  
**Institution:** The Independent Institute of Education (IIE)  
**Academic Year:** 2026

---

## 1. Project Overview

Smart-X is an Internet of Things (IoT) monitoring and management ecosystem designed to support distributed environments such as hydroponic farms, automated utility systems and smart-grid installations.

The system models an environment in which thousands of microcontrollers, including ESP32 devices, continuously transmit different types of telemetry to a central gateway.

Telemetry may contain:

- Floating-point values, such as soil moisture and temperature.
- Integer values, such as power consumption.
- Boolean values, such as valve and device states.
- Timestamped sensor readings used for historical analysis.

The application provides three main functional areas:

1. **Sensor Data Ingestion and Telemetry:** Captures and displays sensor information and telemetry readings.
2. **Real-Time Command Stream and History:** Processes queued telemetry, monitors commands and identifies anomalous behaviour.
3. **Network Topology and Mesh Routing:** Models the IoT network, visualises connections and applies graph algorithms to analyse connectivity and efficient routing.

The project uses a C# ASP.NET Core backend and a Blazor WebAssembly frontend.

---

## 2. System Architecture

Smart-X follows a client-server architecture.

### Backend — SmartX.Api

The backend is responsible for:

- Receiving and processing sensor information.
- Managing telemetry data.
- Validating and analysing incoming readings.
- Processing queued telemetry automatically.
- Maintaining command-stream history.
- Performing historical telemetry queries.
- Modelling the network using a graph.
- Executing BFS and DFS traversal.
- Calculating minimum spanning trees using Prim's and Kruskal's algorithms.
- Exposing functionality through REST API endpoints.

### Frontend — SmartX.Client

The frontend is built using Blazor WebAssembly.

It provides:

- The main dashboard.
- Sensor information and telemetry views.
- Command Stream and History.
- Network topology visualisation.
- BFS and DFS traversal controls.
- Prim and Kruskal MST results.
- Network statistics and connectivity information.

### Communication

The client communicates with the backend through HTTP requests using `HttpClient`.

The backend returns JSON responses, which the frontend deserialises into C# data transfer objects (DTOs).

### High-level architecture

```text
ESP32 / IoT Sensors
        |
        v
ASP.NET Core Web API
        |
        +--------------------------+
        |                          |
        v                          v
Telemetry Processing        Network Topology
        |                          |
        v                          v
Command Stream               Graph Structure
        |                          |
        v                          +---- BFS
History and Analysis               +---- DFS
                                   +---- Prim
                                   +---- Kruskal
        |
        v
Blazor WebAssembly Client
        |
        +---- Main Dashboard
        +---- Sensor Telemetry
        +---- Command Stream
        +---- Network Topology
```

---

## 3. Project Structure

The solution contains two primary projects:

```text
SmartX/
|
+-- SmartX.Api/
|   |
|   +-- Controllers/
|   +-- Models/
|   +-- Services/
|   +-- DataStructures/
|   +-- Program.cs
|   +-- appsettings.json
|
+-- SmartX.Client/
    |
    +-- Pages/
    +-- Layout/
    +-- Services/
    +-- wwwroot/
    +-- Program.cs
```

The backend separates API controllers, models, services and data structures. The frontend separates pages, layout components and API communication services.

---

## 4. Part 1 — Sensor Data Ingestion and Telemetry

The first phase establishes the foundation of the Smart-X ecosystem.

The backend receives sensor information and manages telemetry associated with individual devices.

Telemetry readings are associated with sensor identifiers, allowing the system to retrieve readings for particular devices.

The application supports different telemetry values and timestamps, making it possible to monitor changing environmental and operational conditions.

The frontend presents sensor information and telemetry through the main dashboard.

---

## 5. Part 2 — Real-Time Command Stream and History

Part 2 extends the application with automatic telemetry processing, command-stream management, historical retention and improved anomaly analysis.

### 5.1 Automatic command-stream processing

A background processing service, `TelemetryQueueWorker`, periodically processes queued telemetry.

The worker uses a `PeriodicTimer` to trigger processing approximately every second.

This removes the dependency on a user manually pressing a processing button before queued telemetry can be handled.

### 5.2 Bounded queue processing

The processing service supports a maximum number of telemetry items per processing cycle.

This prevents one processing operation from attempting to drain an unlimited queue.

Bounded processing improves responsiveness and gives the application better control over processing workloads.

### 5.3 Priority-based processing

The command stream supports priority handling, including critical telemetry and FIFO processing.

This allows important telemetry to be processed without treating every queued item as equally urgent.

### 5.4 Normalised severity scoring

Severity scoring was improved to avoid combining unrelated measurement scales directly.

The system uses a more consistent scoring approach so that different types of telemetry can be evaluated without allowing their raw units to distort the result.

### 5.5 Bounded history retention

Command-stream history is limited to a configured maximum of 50,000 items.

This prevents historical records from growing without an upper bound in memory.

A retention strategy is important in IoT systems because sensors can produce a large number of readings over time.

### 5.6 Historical range queries

Historical telemetry can be retrieved using a specified time range.

This allows the application to focus on a particular period instead of retrieving all historical records.

### 5.7 Confidence scoring and minimum evidence

The anomaly and learning logic incorporates confidence scoring and minimum evidence requirements.

This helps prevent the system from treating limited observations as sufficient evidence for a reliable conclusion.

### 5.8 Context-specific learning

The learning logic was improved beyond grouping observations only by metric family.

Context-specific learning provides a more meaningful basis for evaluating telemetry patterns in different operating contexts.

---

## 6. Part 3 — Network Topology and Mesh Routing

Part 3 introduces the network structures and algorithms required to model an IoT mesh environment.

A mesh network allows devices to communicate through neighbouring devices instead of requiring every sensor to connect directly to one central router.

This provides alternative communication paths when a device or connection becomes unavailable.

### 6.1 Graph representation

The network is represented using a weighted, undirected graph.

- Vertices represent IoT sensor devices.
- Edges represent wireless connections between devices.
- Edge weights represent connection costs used by the routing optimisation algorithms.
- Device properties include MAC address, location, category, online status and last-seen timestamp.

The graph uses an adjacency-list representation.

An adjacency list stores the neighbours of each vertex rather than reserving space for every possible connection.

For a sparse mesh network, this is generally more memory-efficient than an adjacency matrix.

If the graph contains V vertices and E edges, adjacency-list storage requires O(V + E) space.

### 6.2 Breadth-First Search (BFS)

BFS explores a graph level by level.

It uses a queue to process the next discovered device.

The implementation:

1. Starts from a selected sensor MAC address.
2. Adds the starting device to the traversal queue.
3. Visits neighbouring devices through active connections.
4. Tracks the traversal level of each device.
5. Identifies devices that cannot be reached from the starting device.
6. Reports whether the reachable network is fully connected.

BFS is useful for discovering which devices are reachable from a selected sensor or gateway.

Its time complexity with an adjacency-list graph is O(V + E).

### 6.3 Depth-First Search (DFS)

DFS explores a path as deeply as possible before backtracking.

The implementation uses an iterative stack-based approach.

It:

1. Starts from a selected device.
2. Pushes neighbouring devices onto a stack.
3. Explores unvisited devices.
4. Tracks traversal depth.
5. Identifies unreachable devices.

DFS provides another way to inspect the network's connectivity.

Its time complexity with an adjacency-list graph is O(V + E).

### 6.4 Minimum Spanning Tree (MST)

A Minimum Spanning Tree connects all reachable vertices in a connected weighted graph while minimising the total edge weight and avoiding cycles.

For disconnected networks, Smart-X calculates a minimum spanning forest, producing a minimum spanning tree for each connected component.

This is important because a real IoT deployment may contain disconnected devices or isolated groups.

The application implements two algorithms.

#### Prim's algorithm

Prim's algorithm grows a spanning tree by repeatedly selecting the lowest-weight available edge that connects the current tree to another vertex.

A priority queue is used to select the next lowest-weight candidate.

Its typical complexity with an adjacency list and binary heap is O((V + E) log V).

#### Kruskal's algorithm

Kruskal's algorithm sorts candidate edges by weight and adds an edge only when it does not create a cycle.

Smart-X uses a Disjoint Set Union (DSU), also known as Union-Find, to track connected components.

The implementation uses:

- Path compression.
- Union by rank.
- Cycle prevention.

Its typical complexity is O(E log E), with the sorting operation dominating the overall cost.

### 6.5 Comparing Prim and Kruskal

Both algorithms calculate minimum spanning forests, but they approach the problem differently.

Prim grows a solution from a starting component, while Kruskal considers edges in ascending weight order.

The dashboard displays the results of both algorithms, including:

- Total network weight.
- Selected connection count.
- Component count.
- Whether the network is fully connected.
- Selected edges.

The results allow the user to compare the calculated connection costs.

### 6.6 Network topology dashboard

The Network Topology page provides:

- Total device count.
- Online and offline device counts.
- Wireless connection counts.
- A visual representation of devices and connections.
- BFS and DFS controls.
- Traversal results and unreachable devices.
- Prim and Kruskal results.
- Minimum spanning forest comparison.
- Visual highlighting of selected MST connections.

The page communicates with the backend through the `TelemetryApiService`.

---

## 7. Important API Endpoints

The network topology controller exposes endpoints under:

`/api/network-topology`

| Endpoint | Purpose |
|---|---|
| `GET /api/network-topology` | Retrieves the topology snapshot |
| `GET /api/network-topology/nodes` | Retrieves network devices |
| `GET /api/network-topology/edges` | Retrieves wireless connections |
| `GET /api/network-topology/nodes/{macAddress}/neighbours` | Retrieves neighbouring devices |
| `GET /api/network-topology/bfs/{macAddress}` | Executes BFS |
| `GET /api/network-topology/dfs/{macAddress}` | Executes DFS |
| `GET /api/network-topology/mst/prim` | Calculates Prim's minimum spanning forest |
| `GET /api/network-topology/mst/kruskal` | Calculates Kruskal's minimum spanning forest |
| `POST /api/network-topology/edges` | Adds a wireless connection |
| `DELETE /api/network-topology/edges/{sourceMac}/{targetMac}` | Removes a wireless connection |
| `PUT /api/network-topology/nodes/{macAddress}/status` | Updates a device's online status |

The historical telemetry endpoints are provided by the relevant telemetry controller. Refer to the controller route definitions in the source code for the exact historical-query URL.

---

## 8. Historical Telemetry and Tree Structures

Historical telemetry requires an efficient way to locate readings by timestamp.

A balanced search tree is suitable because it maintains ordered data while supporting efficient insertion and retrieval.

Smart-X includes an AVL tree implementation for timestamped historical telemetry.

### 8.1 AVL tree

An AVL tree is a self-balancing Binary Search Tree.

For every node, the heights of its left and right subtrees may differ by at most one.

When insertion causes the tree to become unbalanced, rotations restore the AVL balance property.

The four rotation cases are:

- Left-left.
- Right-right.
- Left-right.
- Right-left.

### 8.2 Why AVL was selected

Historical telemetry can contain a large number of timestamped readings.

A normal unbalanced Binary Search Tree can become skewed, potentially degrading search performance to O(n).

An AVL tree maintains logarithmic height, giving:

- Search: O(log n).
- Insertion: O(log n).
- Range query: O(log n + k), where k is the number of returned records, when the implementation prunes subtrees outside the requested range.

This makes it suitable for timestamp-based historical queries.

### 8.3 Historical range queries

A time-window query retrieves readings between a specified start and end timestamp.

For example, an operator could request soil-moisture readings from 14:00 to 16:00 for a particular sensor.

Instead of scanning an entire unorganised collection, the AVL tree can use timestamp ordering to avoid examining irrelevant subtrees.

This improves query efficiency, particularly as the number of stored readings increases.

---

## 9. Development Storage and Configuration

The project supports local development storage.

The Development configuration is used to allow the API to be developed and tested without requiring an active paid Azure subscription.

Azure storage integration remains available as a configuration option outside Development.

The application should be configured using the appropriate connection settings for the selected environment.

Do not commit real storage credentials, connection strings, passwords or API secrets to GitHub.

Use environment variables or appropriate secret-management mechanisms for sensitive configuration.

---

## 10. Requirements

Install the following:

- .NET 10 SDK.
- Visual Studio 2022 or a compatible C# development environment.
- Visual Studio Code or another suitable editor for the Blazor client.
- Git.
- A modern web browser.

Verify the SDK installation:

```powershell
dotnet --version
```

---

## 11. How to Restore, Build and Run

### Step 1 — Clone the repository

```powershell
git clone <YOUR-GITHUB-REPOSITORY-URL>
cd SmartX
```

Replace the repository URL with the actual GitHub repository address.

### Step 2 — Restore dependencies

```powershell
dotnet restore
```

### Step 3 — Build the backend API

```powershell
dotnet build .\SmartX.Api\SmartX.Api.csproj
```

### Step 4 — Start the backend API

Open a terminal in the solution directory:

```powershell
dotnet run --project .\SmartX.Api\SmartX.Api.csproj
```

Check the terminal output for the API's actual listening address.

The default development API address used by the client is:

`http://localhost:5212/`

If the API starts on a different port, update the client's `ApiBaseUrl` configuration accordingly.

### Step 5 — Build the frontend

Open a second terminal:

```powershell
dotnet build .\SmartX.Client\SmartX.Client.csproj
```

### Step 6 — Start the frontend

```powershell
dotnet run --project .\SmartX.Client\SmartX.Client.csproj
```

Open the client address displayed in the terminal.

The development client has previously used:

`http://localhost:5221`

The actual address may vary according to the project's launch settings.

### Step 7 — Test the application

Verify that:

1. The API starts successfully.
2. The frontend opens.
3. The dashboard can retrieve sensor and telemetry information.
4. The Command Stream displays and processes telemetry.
5. Historical telemetry queries return the expected readings.
6. The Network Topology page retrieves the network.
7. BFS and DFS return traversal results.
8. Prim and Kruskal return minimum spanning forest results.

---

## 12. Testing and Validation

Testing should include both normal and edge-case scenarios.

### Telemetry and command-stream testing

- Submit valid telemetry.
- Verify automatic queue processing.
- Verify bounded processing.
- Confirm critical telemetry receives priority.
- Verify history retention.
- Test time-window queries.
- Test confidence and minimum-evidence behaviour.

### Historical tree testing

- Insert timestamped readings.
- Insert readings in a non-chronological order.
- Query a selected time range.
- Verify readings outside the range are excluded.
- Test empty results.
- Test boundary timestamps.
- Verify the tree remains balanced after insertion.

### Graph testing

- Create multiple devices.
- Add weighted connections.
- Execute BFS and DFS.
- Test disconnected components.
- Set a device offline.
- Verify unreachable-device reporting.

### MST testing

- Execute Prim's algorithm.
- Execute Kruskal's algorithm.
- Compare their total weights on the same connected graph.
- Test disconnected networks.
- Confirm inactive and offline connections are excluded.
- Confirm cycles are not included in the resulting forest.

---

## 13. Complexity Summary

| Structure / Algorithm | Typical time complexity |
|---|---|
| AVL search | O(log V) |
| AVL insertion | O(log V) |
| AVL range query | O(log V + k) |
| Adjacency-list storage | O(V + E) |
| BFS | O(V + E) |
| DFS | O(V + E) |
| Prim with binary heap | O((V + E) log V) |
| Kruskal | O(E log E) |

Here, V represents the number of vertices, E the number of edges, and k the number of records returned by a range query.

Actual performance depends on the implementation, data distribution, and number of returned records.

---

## 14. Future Improvements and Technology Recommendations

The Smart-X ecosystem could be extended using the following technologies.

### Azure IoT Hub

Azure IoT Hub could provide managed device connectivity, authentication, telemetry ingestion and device management.

It would support a larger distributed deployment without requiring the application to implement every device-communication feature itself.

### Azure Event Hubs

Event Hubs could support high-volume telemetry ingestion and stream processing.

This would be useful when thousands of sensors transmit data continuously.

### Azure Cosmos DB

Cosmos DB could provide scalable distributed storage for device information and telemetry metadata.

A suitable partitioning strategy would be important to avoid hot partitions.

### Redis

Redis could cache frequently requested device information, network snapshots and dashboard statistics.

This could reduce repeated database queries and improve response times.

### GitHub Actions

GitHub Actions could automate:

- Dependency restoration.
- Backend builds.
- Frontend builds.
- Automated testing.
- Deployment workflows.

This would improve reliability and help prevent broken changes from reaching the main branch.

### Docker and Docker Compose

Containerisation could simplify deployment by packaging the API and its dependencies into reproducible environments.

Docker Compose could coordinate the API, frontend and supporting services during local development.

### Home Assistant

Home Assistant could provide an integration point for smart-home and building-automation environments.

This could expand Smart-X beyond its original sensor-monitoring use cases.

### AI-assisted telemetry analysis

An LLM-based service could summarise telemetry anomalies and generate human-readable daily operational reports.

It should operate as an additional analysis layer rather than replacing deterministic validation, threshold rules or safety-critical controls.

---
## 15.Screenshot

<img width="1365" height="767" alt="Screenshot 2026-10-17 140716" src="https://github.com/user-attachments/assets/5ead6942-8c7e-4eb7-ba74-444232fa0cce" />
Azure account is expired so storage account was disabled hence I used local storage. 
## Note
- co pilot was used to push latest projects to github

## 16.Youtube link https://youtu.be/nCSyYTDcfjI

  
## 17. Conclusion

Smart-X brings together sensor ingestion, telemetry processing, historical analysis and network topology management in a unified IoT application.

Part 1 established the sensor and telemetry foundation.

Part 2 improved the reliability and responsiveness of telemetry processing through automatic queue handling, bounded processing, improved severity scoring, historical retention and context-aware anomaly analysis.

Part 3 extended the ecosystem through an AVL tree for timestamped historical queries, a graph representation of the mesh network, BFS and DFS traversal, and Prim's and Kruskal's minimum spanning tree algorithms.

The Blazor WebAssembly client provides a unified interface for interacting with these capabilities through the ASP.NET Core API.

The project demonstrates how data structures, algorithms and modern application architecture can be applied to a practical IoT monitoring and network-management scenario.
