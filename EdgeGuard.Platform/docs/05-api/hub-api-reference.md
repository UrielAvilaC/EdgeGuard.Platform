# Hub API Reference

## Overview

The EdgeGuard Hub exposes a RESTful HTTP API consumed by the SPA and by administrative tooling. All endpoints (except authentication) require a valid JWT bearer token.

**Base URL (production):** `https://<hub-host>/api`  
**Base URL (development):** `http://localhost:5000/api`

### Authentication Header

```
Authorization: Bearer <accessToken>
```

### Pagination Response Envelope

All paginated endpoints return the following envelope:

```json
{
  "items": [ ... ],
  "totalCount": 1250,
  "page": 1,
  "pageSize": 20,
  "totalPages": 63
}
```

### cURL — Login Example

```bash
curl -X POST https://<hub-host>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@hospital.org", "password": "S3cur3P@ss!" }'
```

---

## Auth — `AuthController`

No authorization required unless noted.

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/login` | None | Authenticate with email and password |
| POST | `/api/auth/refresh` | None | Obtain a new access token using a refresh token |
| POST | `/api/auth/revoke` | Bearer | Revoke the current refresh token |
| GET | `/api/auth/me` | Bearer | Retrieve the authenticated user's profile |
| POST | `/api/auth/bootstrap` | Bootstrap token | First-run: create the initial admin user |

### POST `/api/auth/login`

**Request body:**

```json
{
  "email": "admin@hospital.org",
  "password": "S3cur3P@ss!"
}
```

**Response `200 OK`:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 3600
}
```

### POST `/api/auth/refresh`

**Request body:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response `200 OK`:** Same shape as login response.

### GET `/api/auth/me`

**Response `200 OK`:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Dr. Ana Martínez",
  "email": "ana.martinez@hospital.org",
  "role": "Radiologist",
  "permissions": ["ViewStudies", "ExportData"]
}
```

---

## Studies — `StudiesController`

Required permission: `ViewStudies`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/studies` | Bearer | Paginated study list with filters |
| GET | `/api/studies/{id}` | Bearer | Single study by internal ID |
| GET | `/api/studies/by-uid/{studyInstanceUid}` | Bearer | Single study by DICOM Study Instance UID |
| GET | `/api/studies/by-patient/{patientId}` | Bearer | All studies for a patient |
| GET | `/api/studies/by-node/{nodeId}` | Bearer | All studies received from a node |
| PUT | `/api/studies/{id}` | Bearer | Update study metadata |
| DELETE | `/api/studies/{id}` | Bearer | Soft-delete study |
| GET | `/api/studies/export/csv` | Bearer | Export filtered result set as CSV |

### GET `/api/studies` — Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number (1-based, default `1`) |
| `pageSize` | integer | Records per page (default `20`, max `100`) |
| `search` | string | Free-text search across patient name, MRN, accession |
| `status` | string | `Pending`, `Sent`, `Failed`, `Cancelled` |
| `sourceNodeId` | guid | Filter by originating node |
| `patientId` | guid | Filter by patient |
| `dateFrom` | ISO 8601 date | Study date range start |
| `dateTo` | ISO 8601 date | Study date range end |
| `isUrgent` | boolean | `true` to return only urgent studies |
| `sortBy` | string | Field name for sorting |
| `sortDir` | string | `asc` or `desc` |

**Response `200 OK`:**

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "studyInstanceUid": "1.2.840.10008.5.1.4.1.1.2",
      "accessionNumber": "ACC-2025-00142",
      "patientId": "...",
      "patientName": "MARTINEZ^ANA",
      "studyDate": "2025-05-14",
      "modality": "CT",
      "status": "Sent",
      "isUrgent": false,
      "sourceNodeId": "...",
      "sourceNodeName": "CT-SALA-1",
      "receivedAt": "2025-05-14T09:32:11Z"
    }
  ],
  "totalCount": 1250,
  "page": 1,
  "pageSize": 20,
  "totalPages": 63
}
```

---

## Patients — `PatientsController`

Required permission: `ViewStudies`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/patients` | Bearer | Paginated patient list with filters |
| GET | `/api/patients/{id}` | Bearer | Single patient |
| POST | `/api/patients` | Bearer | Create patient record |
| PUT | `/api/patients/{id}` | Bearer | Update patient demographics |
| DELETE | `/api/patients/{id}` | Bearer | Soft-delete patient |
| GET | `/api/patients/export/csv` | Bearer | CSV export |
| POST | `/api/patients/import/csv` | Bearer | Bulk import from CSV |

### GET `/api/patients` — Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `pageSize` | integer | Records per page |
| `search` | string | Name, MRN, phone, or email |
| `createdByNodeId` | guid | Filter by node that created the record |
| `isActive` | boolean | Active patients only |
| `hasPhone` | boolean | Patients with phone number |
| `hasEmail` | boolean | Patients with email |
| `sortBy` | string | Sort field |
| `sortDir` | string | `asc` or `desc` |

### POST `/api/patients`

**Request body:**

```json
{
  "mrn": "MRN-00412",
  "firstName": "Ana",
  "lastName": "Martínez",
  "dateOfBirth": "1985-03-22",
  "sex": "F",
  "phone": "+52-55-1234-5678",
  "email": "ana.martinez@example.com",
  "address": "Calle Reforma 123, CDMX"
}
```

**Response `201 Created`:** Full patient object with generated `id`.

---

## Nodes — `NodesController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/nodes` | Bearer | List all registered nodes |
| GET | `/api/nodes/{id}` | Bearer | Single node detail |
| POST | `/api/nodes` | Bearer | Register a new node |
| PUT | `/api/nodes/{id}` | Bearer | Update node configuration |
| DELETE | `/api/nodes/{id}` | Bearer | Remove node |
| POST | `/api/nodes/{id}/sync` | Bearer | Push all configuration to node |

### POST `/api/nodes`

**Request body:**

```json
{
  "name": "CT-SALA-1",
  "description": "CT scanner in radiology room 1",
  "ipAddress": "192.168.10.50",
  "port": 5001,
  "aeTitle": "EDGEGUARD_CT1",
  "isEnabled": true
}
```

**Response `201 Created`:** Full node object.

### POST `/api/nodes/{id}/sync`

Pushes the current PACS destinations, DICOM routing rules, and system configuration to the target node. Returns immediately; sync is performed asynchronously on the node.

**Response `204 No Content`**

---

## PACS Servers — `PacsServersController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/pacs-servers` | Bearer | List all PACS server definitions |
| POST | `/api/pacs-servers` | Bearer | Add PACS server |
| PUT | `/api/pacs-servers/{id}` | Bearer | Update PACS server |
| DELETE | `/api/pacs-servers/{id}` | Bearer | Remove PACS server |
| POST | `/api/pacs-servers/{id}/assign-node/{nodeId}` | Bearer | Associate PACS to a node |
| POST | `/api/pacs-servers/{id}/test-echo/{nodeId}` | Bearer | Trigger C-ECHO connectivity test |

### POST `/api/pacs-servers`

**Request body:**

```json
{
  "name": "PACS-Principal",
  "description": "Main hospital PACS",
  "ipAddress": "10.0.1.200",
  "port": 11112,
  "aeTitle": "PACS_MAIN",
  "callingAeTitle": "EDGEGUARD_HUB"
}
```

### POST `/api/pacs-servers/{id}/test-echo/{nodeId}`

Instructs the specified node to send a DICOM C-ECHO to this PACS server and reports the result.

**Response `200 OK`:**

```json
{
  "success": true,
  "responseTimeMs": 42,
  "message": "C-ECHO succeeded"
}
```

---

## Node DICOM Routing Rules — `NodeDicomRoutingRulesController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/nodes/{nodeId}/dicom-routing-rules` | Bearer | List routing rules for a node |
| POST | `/api/nodes/{nodeId}/dicom-routing-rules` | Bearer | Create routing rule |
| PUT | `/api/nodes/{nodeId}/dicom-routing-rules/{id}` | Bearer | Update routing rule |
| DELETE | `/api/nodes/{nodeId}/dicom-routing-rules/{id}` | Bearer | Delete routing rule |
| POST | `/api/nodes/{nodeId}/dicom-routing-rules/{id}/enable` | Bearer | Enable rule |
| POST | `/api/nodes/{nodeId}/dicom-routing-rules/{id}/disable` | Bearer | Disable rule |
| PUT | `/api/nodes/{nodeId}/dicom-routing-rules/{id}/priority` | Bearer | Update rule evaluation priority |

### POST `/api/nodes/{nodeId}/dicom-routing-rules`

**Request body:**

```json
{
  "name": "Route CT to PACS-Principal",
  "priority": 10,
  "isEnabled": true,
  "conditions": [
    { "field": "Modality", "operator": "Equals", "value": "CT" }
  ],
  "pacsDestinationIds": ["<pacsServerId>"]
}
```

---

## HL7 Status — `Hl7StatusController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/hl7/messages` | Bearer | Paginated HL7 message queue |
| GET | `/api/hl7/messages/{id}` | Bearer | Single HL7 message detail |
| POST | `/api/hl7/messages/{id}/reprocess` | Bearer | Re-queue a failed message |

### GET `/api/hl7/messages` — Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `pageSize` | integer | Records per page |
| `status` | string | `Received`, `Processed`, `Failed` |
| `dispatchStatus` | string | `Pending`, `Sent`, `Failed` |
| `messageType` | string | HL7 message type, e.g. `ADT^A01` |
| `dateFrom` | ISO 8601 | Received date range start |
| `dateTo` | ISO 8601 | Received date range end |

---

## Users — `UsersController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/users` | Bearer | List all application users |
| POST | `/api/users` | Bearer | Create user |
| PUT | `/api/users/{id}` | Bearer | Update user details |
| DELETE | `/api/users/{id}` | Bearer | Deactivate user |
| PUT | `/api/users/{id}/role` | Bearer | Change user role |

### POST `/api/users`

**Request body:**

```json
{
  "name": "Dr. Carlos Pérez",
  "email": "cperez@hospital.org",
  "role": "Radiologist",
  "password": "Temp@12345"
}
```

---

## Dashboard — `DashboardController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/dashboard` | Bearer | Platform summary statistics |

**Response `200 OK`:**

```json
{
  "studiesToday": 142,
  "activeNodes": 7,
  "pendingSends": 3,
  "failedSends": 1,
  "lastUpdatedAt": "2025-05-14T10:00:00Z"
}
```

---

## Audit Logs — `AuditLogsController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/audit-logs` | Bearer | Paginated audit log entries |

### GET `/api/audit-logs` — Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | integer | Page number |
| `pageSize` | integer | Records per page |
| `entityType` | string | `Study`, `Patient`, `Node`, `User`, etc. |
| `userId` | guid | Filter by acting user |
| `dateFrom` | ISO 8601 | Date range start |
| `dateTo` | ISO 8601 | Date range end |

---

## HL7 Routing Rules — `RoutingRulesController`

Controls outbound HL7 message routing (distinct from DICOM routing rules).

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/routing-rules` | Bearer | List all HL7 routing rules |
| POST | `/api/routing-rules` | Bearer | Create routing rule |
| PUT | `/api/routing-rules/{id}` | Bearer | Update routing rule |
| DELETE | `/api/routing-rules/{id}` | Bearer | Delete routing rule |

---

## System Settings — `SystemSettingsController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/system-settings` | Bearer | Retrieve all settings key-value pairs |
| PUT | `/api/system-settings/{key}` | Bearer | Update a single setting |

**Response `200 OK` (GET):**

```json
{
  "settings": {
    "DefaultPageSize": "20",
    "StudyRetentionDays": "365",
    "Hl7ListenerEnabled": "true"
  }
}
```

---

## Queue Monitoring — `QueueMonitoringController`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/queue/status` | Bearer | Background job queue statistics |
| POST | `/api/queue/retry-failed` | Bearer | Re-enqueue all failed jobs |

**Response `200 OK` (GET `/api/queue/status`):**

```json
{
  "pendingJobs": 5,
  "processingJobs": 2,
  "failedJobs": 1,
  "completedToday": 318
}
```
