# Node API Reference

## Overview

Each EdgeGuard Node exposes a lightweight internal HTTP API. This API is **not intended for end users or third-party consumers**; it is called exclusively by the Hub to push configuration, synchronize state, and retrieve telemetry. Direct external access should be blocked at the network perimeter.

**Base URL:** `http://<node-ip>:<node-port>` (default port `5001`)

### Authentication

Node API endpoints use a shared secret or mutual TLS configured during node registration. The Hub includes a pre-shared key in the `X-Hub-Api-Key` request header:

```
X-Hub-Api-Key: <nodeApiSecret>
```

The node validates this header on all `/api/*` routes. The `/health` endpoint is unauthenticated by design and intended for infrastructure health checks (load balancers, Kubernetes liveness probes).

---

## Health Check

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/health` | None | Overall node health status |

### GET `/health`

Returns the operational status of all node subsystems. Used by the Hub to determine whether a node is reachable before attempting sync operations.

**Response `200 OK` (healthy):**

```json
{
  "status": "Healthy",
  "timestamp": "2025-05-14T09:45:00Z",
  "components": {
    "dicomServer": {
      "status": "Healthy",
      "description": "DICOM SCP listening on port 11112"
    },
    "hl7Listener": {
      "status": "Healthy",
      "description": "HL7 MLLP listener active on port 2575"
    },
    "database": {
      "status": "Healthy",
      "description": "SQLite connection OK"
    }
  }
}
```

**Response `503 Service Unavailable` (degraded):**

```json
{
  "status": "Degraded",
  "timestamp": "2025-05-14T09:45:00Z",
  "components": {
    "dicomServer": {
      "status": "Unhealthy",
      "description": "DICOM SCP failed to bind port 11112 — address already in use"
    },
    "hl7Listener": { "status": "Healthy", "description": "..." },
    "database": { "status": "Healthy", "description": "..." }
  }
}
```

---

## Configuration Sync

These endpoints receive configuration payloads pushed by the Hub. Each is idempotent — repeated calls with the same payload are safe.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/pacs-destinations/sync` | API Key | Replace node's PACS destination list |
| POST | `/api/dicom-routing-rules/sync` | API Key | Replace node's DICOM routing rule set |
| POST | `/api/configuration/sync` | API Key | Apply general settings to node |

### POST `/api/pacs-destinations/sync`

The Hub sends the complete list of PACS servers assigned to this node. The node replaces its local PACS destination table atomically.

**Request body:**

```json
{
  "destinations": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "PACS-Principal",
      "ipAddress": "10.0.1.200",
      "port": 11112,
      "aeTitle": "PACS_MAIN",
      "callingAeTitle": "EDGEGUARD_CT1",
      "isEnabled": true
    }
  ]
}
```

**Response `204 No Content`**

### POST `/api/dicom-routing-rules/sync`

The Hub sends all active DICOM routing rules for this node ordered by priority.

**Request body:**

```json
{
  "rules": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "name": "Route CT to PACS-Principal",
      "priority": 10,
      "isEnabled": true,
      "conditions": [
        { "field": "Modality", "operator": "Equals", "value": "CT" }
      ],
      "pacsDestinationIds": [
        "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      ]
    }
  ]
}
```

**Response `204 No Content`**

### POST `/api/configuration/sync`

Applies general operational settings to the node (e.g. log level, retry intervals, timeouts).

**Request body:**

```json
{
  "settings": {
    "DicomRetryAttempts": "3",
    "DicomRetryDelaySeconds": "30",
    "Hl7ListenerEnabled": "true",
    "LogLevel": "Information"
  }
}
```

**Response `204 No Content`**

---

## Status and Telemetry

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/dicom-status` | API Key | Current DICOM server state and active connections |
| GET | `/api/pacs-status` | API Key | PACS destination status and last send timestamps |
| GET | `/api/worklist` | API Key | Current worklist entries available to modalities |

### GET `/api/dicom-status`

**Response `200 OK`:**

```json
{
  "isListening": true,
  "listeningPort": 11112,
  "aeTitle": "EDGEGUARD_CT1",
  "activeAssociations": 2,
  "studiesReceivedToday": 47,
  "lastStudyReceivedAt": "2025-05-14T09:30:00Z"
}
```

### GET `/api/pacs-status`

**Response `200 OK`:**

```json
{
  "destinations": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "PACS-Principal",
      "isReachable": true,
      "lastEchoAt": "2025-05-14T09:00:00Z",
      "lastSendAt": "2025-05-14T09:31:00Z",
      "pendingStudies": 0,
      "failedStudies": 0
    }
  ]
}
```

### GET `/api/worklist`

Returns the current modality worklist entries that the node's DICOM Worklist SCP will serve to querying modalities.

**Response `200 OK`:**

```json
{
  "entries": [
    {
      "accessionNumber": "ACC-2025-00143",
      "patientId": "MRN-00412",
      "patientName": "MARTINEZ^ANA",
      "scheduledProcedureStepStartDate": "20250514",
      "scheduledProcedureStepStartTime": "1000",
      "modality": "CT",
      "scheduledAeTitle": "EDGEGUARD_CT1",
      "studyInstanceUid": "1.2.840.10008.5.1.4.1.1.2.999"
    }
  ],
  "count": 1
}
```

---

## Hub-Initiated Actions

These endpoints allow the Hub to trigger node-side operations.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/pacs-destinations/sync` | API Key | See Configuration Sync above |
| POST | `/api/dicom-routing-rules/sync` | API Key | See Configuration Sync above |
| POST | `/api/configuration/sync` | API Key | See Configuration Sync above |

> **Note:** All sync operations are synchronous from the Hub's perspective — the node applies the configuration before returning the response. The Hub's `POST /api/nodes/{id}/sync` endpoint orchestrates calling all three sync routes in the correct order.

---

## Error Responses

Node API errors follow the standard ProblemDetails format (see [error-codes.md](./error-codes.md)):

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Missing or invalid X-Hub-Api-Key header",
  "instance": "/api/pacs-destinations/sync"
}
```
