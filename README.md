# Smart-X IoT Mesh Ecosystem

## Overview

Smart-X is a simulated Internet of Things (IoT) mesh ecosystem developed as part of the Smart-X Portfolio of Evidence (PoE). The system demonstrates how sensor devices can be managed, monitored and connected to a central backend through a web-based application programming interface (API).

The project focuses on practical IoT backend functionality, cloud-style storage integration, telemetry processing, sensor health monitoring, document management and containerisation.

The solution is implemented using ASP.NET Core for the backend API and Blazor for the client application.

---

## Project Structure

The Smart-X solution contains two main projects:

```text
SmartX
│
├── SmartX.Api
│   ├── Controllers
│   ├── Models
│   ├── Services
│   ├── Dockerfile
│   ├── Program.cs
│   └── appsettings.json
│
└── SmartX.Client
    ├── Pages
    ├── Layout
    ├── wwwroot
    └── Program.cs
```

### SmartX.Api

`SmartX.Api` provides the backend services for the Smart-X ecosystem.

It manages:

* Sensor registration and management
* Sensor category filtering
* Telemetry collection
* Mock telemetry generation
* Telemetry anomaly detection
* Sensor health information
* Sensor file management
* Staff document management
* Azure Table Storage integration
* Azure File Share integration
* API validation and error handling

### SmartX.Client

`SmartX.Client` provides the Blazor web interface used to interact with the Smart-X backend.

The client provides pages for working with sensor and telemetry information through the API.

---

# Main Features

## Sensor Management

The API provides CRUD operations for Smart-X sensors.

Sensors contain:

* MAC address
* Location
* Category

Example sensor categories include:

```text
Temperature
Humidity
Pressure
```

The API supports retrieving all sensors as well as filtering sensors by category.

Example:

```http
GET /api/Sensor
```

```http
GET /api/Sensor?category=Temperature
```

---

## Telemetry

Smart-X supports telemetry collection from sensors.

Telemetry records contain information such as:

* Sensor MAC address
* Metric
* Value
* Timestamp

Telemetry can be submitted through the API and retrieved for individual sensors.

The system also provides mock telemetry generation to assist with testing.

Example:

```http
POST /api/Telemetry
```

```http
GET /api/Telemetry/{sensorMacAddress}
```

Mock telemetry can be generated using:

```http
POST /api/Telemetry/seed/{sensorMacAddress}?count=100
```

The system also provides telemetry anomaly detection:

```http
GET /api/Telemetry/anomalies/{sensorMacAddress}
```

---

# Sensor Health Monitoring

Smart-X provides a sensor health endpoint that can be used to retrieve health information associated with a sensor.

```http
GET /api/SensorHealth/{sensorMacAddress}
```

This supports the monitoring aspect of the Smart-X IoT ecosystem.

---

# Azure Table Storage

Sensor and telemetry-related information is integrated with Azure Storage technologies.

During local development, the project uses **Azurite** to emulate Azure Storage services locally.

This allows the application to demonstrate cloud-style storage functionality without requiring a production Azure environment during development and testing.

---

# Azure File Share Integration

Smart-X includes File Share functionality for document and sensor-file storage.

The document API provides:

```http
POST /api/documents/upload
GET /api/documents
GET /api/documents/download/{fileName}
```

The implementation supports stream-based file transfers and records file metadata such as:

* File name
* File size
* Upload date
* Content type

File type and size validation are also applied during uploads.

The local development environment uses an Azurite File Share to simulate Azure Files.

---

# Sensor File Management

The application also provides sensor-specific file management endpoints.

```http
POST /api/sensor-files/{macAddress}
```

```http
GET /api/sensor-files/{macAddress}
```

```http
GET /api/sensor-files/{macAddress}/download/{fileName}
```

These endpoints support uploading, listing and downloading files associated with individual sensors.

---

# API Endpoints

| Function             | Method | Endpoint                                               |
| -------------------- | ------ | ------------------------------------------------------ |
| Get all sensors      | GET    | `/api/Sensor`                                          |
| Filter sensors       | GET    | `/api/Sensor?category={category}`                      |
| Get sensor           | GET    | `/api/Sensor/{macAddress}`                             |
| Create sensor        | POST   | `/api/Sensor`                                          |
| Update sensor        | PUT    | `/api/Sensor/{macAddress}`                             |
| Delete sensor        | DELETE | `/api/Sensor/{macAddress}`                             |
| Add telemetry        | POST   | `/api/Telemetry`                                       |
| Get telemetry        | GET    | `/api/Telemetry/{sensorMacAddress}`                    |
| Seed telemetry       | POST   | `/api/Telemetry/seed/{sensorMacAddress}?count={count}` |
| Detect anomalies     | GET    | `/api/Telemetry/anomalies/{sensorMacAddress}`          |
| Sensor health        | GET    | `/api/SensorHealth/{sensorMacAddress}`                 |
| Upload document      | POST   | `/api/documents/upload`                                |
| List documents       | GET    | `/api/documents`                                       |
| Download document    | GET    | `/api/documents/download/{fileName}`                   |
| Upload sensor file   | POST   | `/api/sensor-files/{macAddress}`                       |
| List sensor files    | GET    | `/api/sensor-files/{macAddress}`                       |
| Download sensor file | GET    | `/api/sensor-files/{macAddress}/download/{fileName}`   |

---

# Testing

API functionality was tested using **Postman**.

A dedicated Postman collection named:

```text
SmartX IoT API
```

was created to organise the API tests.

The collection includes requests for:

* Sensors
* Sensor category filtering
* Telemetry
* Sensor health
* Documents
* Sensor files

Successful API testing was performed against the local ASP.NET Core API.

Examples of successfully tested operations include:

```text
GET /api/Sensor
GET /api/Sensor?category=Temperature
GET /api/documents
GET /api/documents/download/SmartX-Test.txt
GET /api/SensorHealth/{sensorMacAddress}
GET /api/Telemetry/{sensorMacAddress}
```

The API returned successful HTTP responses during testing.

---

# Docker

The `SmartX.Api` project includes a Dockerfile for containerisation.

Docker is being used to support deployment and portability of the backend API.

The final deployment stage includes building and running the Smart-X API as a container and publishing the resulting image to Docker Hub.

---

# Security and Reliability

Security and reliability are considered throughout the implementation.

The project includes:

* Input validation
* MAC address validation
* File size validation
* MIME/content-type validation
* Controlled API error responses
* Logging for important file operations
* Separation between the client and backend API
* Cloud-style storage abstraction
* Containerisation for consistent deployment

Further production hardening would include authentication and authorisation, HTTPS enforcement, rate limiting, secret management and additional monitoring.

---

# Local Development

## Requirements

The following tools are required to run the project:

* .NET SDK
* Visual Studio or another compatible .NET development environment
* Docker Desktop
* Postman
* Azurite for local Azure Storage emulation

---

## Running the API

Open the solution and run the `SmartX.Api` project.

The local API is currently configured to run on:

```text
http://localhost:5212
```

The exact port may differ depending on the local development configuration.

---

## Running the Client

Run the `SmartX.Client` project.

The Blazor client can then communicate with the Smart-X API.

---

# Development Progress

The project has been developed incrementally with Git version control.

Major implementation checkpoints have been committed to GitHub throughout development.

The repository currently contains multiple development commits documenting the progression of the Smart-X implementation.

---

# Future Improvements

Potential future improvements include:

* Production Azure deployment
* Authentication and role-based authorisation
* Real IoT device connectivity
* MQTT integration
* Real-time telemetry dashboards
* Advanced anomaly detection
* Database persistence improvements
* Automated API testing
* CI/CD deployment pipelines
* Application monitoring and logging
* Production HTTPS configuration
* Scalable cloud deployment

---

# Conclusion

Smart-X demonstrates a practical IoT ecosystem in which sensors, telemetry, health monitoring and document storage can be managed through a central API.

The project combines ASP.NET Core, Blazor, Azure Storage concepts, Azurite, Postman and Docker to demonstrate the development and deployment of a modular IoT-oriented software solution.
