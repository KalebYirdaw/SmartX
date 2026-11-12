# Part 2 Lecturer Feedback — Implementation Ledger

| Feedback | Change made | Verification to perform |
|---|---|---|
| Active pillar only highlights a route | This refers to the Part 3 topology feature. Part 2 does not claim to deliver graph route tracing; Part 3 still needs real graph paths and route recovery. | In Part 3, demonstrate BFS/DFS, disconnected-node detection and MST paths. |
| Navigation closes on any click inside it | Removed the click handler from the whole menu container. Only navigation links close the menu. | Open the mobile menu, click empty menu space, then select a link. |
| Command stream has no automatic refresh | Added a three-second periodic status/suggestion refresh; timer is cancelled on component disposal. | Leave the command page open and observe status refresh without clicking Refresh. |
| Live telemetry waits for manual processing | Added `TelemetryQueueWorker`, a hosted background service that processes queued telemetry every second. | Submit a reading and observe queue counts fall without manual processing. |
| Drain takes everything | Queue processor accepts a maximum batch size. Background worker uses 100; API accepts 1–1,000. | Enqueue more than 100 readings and verify processing spans ticks. |
| Severity mixes unrelated scales | Replaced raw unit comparisons with proportional, dimensionless deviation from each metric's own threshold. | Compare critical moisture, power and temperature readings; priority uses relative deviation. |
| History is never trimmed | In-memory history is capped at 50,000 readings; oldest timestamp buckets are removed first. | Review `historyCount` and retention behavior under a large test load. |
| No range/time-window query | Added `GET /api/command-stream/history/range` filtered by sensor, metric and inclusive UTC range. | Query a known sensor/metric/time interval and verify only matching rows return. |
| No confidence score | Added a `Confidence` value to learned suggestions and display it as a percentage. | Trigger a learned suggestion and inspect the percentage. |
| No minimum evidence | Learned suggestions require at least three supporting examples and at least 60% confidence. | Verify one or two examples do not create a learned suggestion. |
| Learning per metric family, not context | Pattern keys include device category and metric family, in addition to command and value. | Use two device categories with the same metric and confirm their learned patterns do not cross over. |

## Important scope note

The Part 3 assignment requires a functioning graph model, BFS or DFS reachability, offline-node identification, route tracing and Prim's or Kruskal's minimum spanning tree. A highlighted navigation route alone is not evidence of these algorithms. This Part 2 package does not claim those Part 3 marks have been completed.

## Build and runtime verification

This package was prepared from the uploaded source. Run `dotnet restore` and `dotnet build` for both projects in an environment with the .NET 10 SDK. Start Azurite/local file storage as described in the README, then start the API and client and complete the manual verification checklist. A successful build/runtime test must be recorded from the user's development environment before submission.
