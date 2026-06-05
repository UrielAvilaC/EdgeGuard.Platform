# Error Codes and Responses

## Standard Error Format — RFC 7807 ProblemDetails

All Hub API errors are returned in the [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807) format with `Content-Type: application/problem+json`.

```json
{
  "type": "https://edgeguard.io/errors/<error-slug>",
  "title": "Human-readable error title",
  "status": 422,
  "detail": "A specific explanation of what went wrong in this request.",
  "instance": "/api/studies/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Field | Description |
|-------|-------------|
| `type` | Stable URI identifying the error class; safe to match in client code |
| `title` | Short human-readable summary; may be localized |
| `status` | HTTP status code (mirrors the HTTP response status) |
| `detail` | Specific description of this error occurrence |
| `instance` | The request URI that produced the error |

Validation errors add an `errors` extension object:

```json
{
  "type": "https://edgeguard.io/errors/validation-failed",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more fields did not pass validation.",
  "instance": "/api/patients",
  "errors": {
    "email": ["'email' is not a valid email address."],
    "dateOfBirth": ["'dateOfBirth' must be in the past."]
  }
}
```

---

## HTTP Status Codes

| Code | Name | When Used |
|------|------|-----------|
| `200 OK` | Success | Successful GET, PUT |
| `201 Created` | Created | Successful POST that creates a resource |
| `204 No Content` | No Content | Successful DELETE, action endpoints (sync, revoke) |
| `400 Bad Request` | Bad Request | Malformed request body or missing required fields |
| `401 Unauthorized` | Unauthorized | Missing, expired, or invalid JWT or API key |
| `403 Forbidden` | Forbidden | Valid JWT but insufficient permissions |
| `404 Not Found` | Not Found | Requested resource does not exist |
| `409 Conflict` | Conflict | Duplicate entity; resource already exists |
| `422 Unprocessable Entity` | Validation Failed | Request body is syntactically valid but semantically invalid |
| `429 Too Many Requests` | Rate Limit Exceeded | Client has sent too many requests in a given time window |
| `500 Internal Server Error` | Server Error | Unexpected server-side failure |

---

## Common Error Scenarios

### 401 — Invalid or Expired JWT

Returned when the `Authorization` header is absent, the token signature is invalid, or the token has expired.

```json
{
  "type": "https://edgeguard.io/errors/invalid-token",
  "title": "Unauthorized",
  "status": 401,
  "detail": "The access token has expired. Obtain a new token using your refresh token.",
  "instance": "/api/studies"
}
```

**Client action:** Call `POST /api/auth/refresh` and retry with the new access token. If the refresh token is also expired, redirect the user to the login page.

---

### 403 — Insufficient Permissions

The authenticated user's role does not grant the required permission for this action.

```json
{
  "type": "https://edgeguard.io/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "The 'ManageUsers' permission is required to perform this action.",
  "instance": "/api/users"
}
```

**Client action:** Display an access-denied message; do not retry.

---

### 404 — Resource Not Found

The requested entity does not exist or has been soft-deleted.

```json
{
  "type": "https://edgeguard.io/errors/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Study with id '3fa85f64-5717-4562-b3fc-2c963f66afa6' was not found.",
  "instance": "/api/studies/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

### 409 — Duplicate Entity

A resource with a conflicting unique key already exists.

```json
{
  "type": "https://edgeguard.io/errors/duplicate-entity",
  "title": "Conflict",
  "status": 409,
  "detail": "A PACS server with AE title 'PACS_MAIN' already exists.",
  "instance": "/api/pacs-servers"
}
```

---

### 422 — Validation Error

Request body is structurally valid JSON but one or more field values fail business validation rules.

```json
{
  "type": "https://edgeguard.io/errors/validation-failed",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more fields did not pass validation.",
  "instance": "/api/patients",
  "errors": {
    "mrn": ["'mrn' must not be empty."],
    "dateOfBirth": ["'dateOfBirth' must be a date in the past."],
    "email": ["'email' is not a valid email address."]
  }
}
```

---

### 429 — Rate Limit Exceeded

The client has exceeded the configured request rate limit.

```json
{
  "type": "https://edgeguard.io/errors/rate-limit-exceeded",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "You have exceeded the rate limit of 100 requests per minute.",
  "instance": "/api/studies"
}
```

#### Rate Limit Response Headers

| Header | Description |
|--------|-------------|
| `X-RateLimit-Limit` | Maximum requests allowed in the current window |
| `X-RateLimit-Remaining` | Remaining requests in the current window |
| `Retry-After` | Seconds until the rate limit window resets |

**Example headers:**

```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 0
Retry-After: 47
```

---

### 500 — Internal Server Error

An unexpected error occurred on the server. A correlation ID is included for log tracing.

```json
{
  "type": "https://edgeguard.io/errors/internal-error",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please contact support with the correlation ID.",
  "instance": "/api/studies/export/csv",
  "correlationId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

**Client action:** Log the `correlationId`; display a generic error message to the user; do not expose server internals.

---

## Domain-Specific Error Examples

These are application-level errors that use domain-specific `type` URIs to enable precise client handling.

### Study Not Found

```json
{
  "type": "https://edgeguard.io/errors/study-not-found",
  "title": "Study Not Found",
  "status": 404,
  "detail": "No study with Study Instance UID '1.2.840.10008.5.1.4.1.1.2.999' exists in this system.",
  "instance": "/api/studies/by-uid/1.2.840.10008.5.1.4.1.1.2.999"
}
```

### Patient Already Merged

```json
{
  "type": "https://edgeguard.io/errors/patient-already-merged",
  "title": "Patient Already Merged",
  "status": 409,
  "detail": "Patient 'MRN-00412' has already been merged into patient 'MRN-00388'. Operations on merged patients must target the surviving record.",
  "instance": "/api/patients/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "survivingPatientId": "7c9e6679-7425-40de-944b-e07fc1f90ae7"
}
```

### Node Offline

```json
{
  "type": "https://edgeguard.io/errors/node-offline",
  "title": "Node Offline",
  "status": 409,
  "detail": "Node 'CT-SALA-1' is not reachable. Sync operation cancelled. Verify network connectivity and that the EdgeGuard Node service is running.",
  "instance": "/api/nodes/7c9e6679-7425-40de-944b-e07fc1f90ae7/sync",
  "lastSeenAt": "2025-05-14T08:00:00Z"
}
```

### HL7 Message Reprocess — Already Processed

```json
{
  "type": "https://edgeguard.io/errors/message-already-processed",
  "title": "Message Already Processed",
  "status": 409,
  "detail": "HL7 message 'MSG-00891' is in 'Processed' status and cannot be reprocessed. Only messages in 'Failed' status are eligible.",
  "instance": "/api/hl7/messages/00891/reprocess"
}
```

### Invalid Bootstrap Token

```json
{
  "type": "https://edgeguard.io/errors/invalid-bootstrap-token",
  "title": "Invalid Bootstrap Token",
  "status": 401,
  "detail": "The bootstrap token is invalid, has already been used, or has expired.",
  "instance": "/api/auth/bootstrap"
}
```

---

## Error Handling Best Practices

1. **Always check `type`** rather than `detail` text for programmatic error handling. The `type` URI is stable across versions; `detail` may be localized or rephrased.
2. **Log `correlationId`** on 5xx errors so support engineers can locate the corresponding server log entry.
3. **Retry on 429** after the number of seconds specified in the `Retry-After` header — do not retry immediately.
4. **Do not retry on 400, 403, 404, 409, or 422** — these indicate a client-side issue that will not resolve with retries.
5. **Retry on 500** with exponential back-off (maximum 3 attempts) — unexpected server errors may be transient.
