# Authentication Endpoints

## Overview

EdgeGuard Hub uses JSON Web Tokens (JWT) for stateless authentication combined with refresh tokens for long-lived sessions. This document describes the complete authentication lifecycle, token structure, storage recommendations, the first-run bootstrap flow, and role-based access control.

---

## Login Flow

The client submits credentials and receives a short-lived access token and a longer-lived refresh token.

### Request

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@hospital.org",
  "password": "S3cur3P@ss!"
}
```

### Response `200 OK`

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZmE4NWY2NC01NzE3LTQ1NjItYjNmYy0yYzk2M2Y2NmFmYTYiLCJuYW1lIjoiQWRtaW4gVXNlciIsImVtYWlsIjoiYWRtaW5AaG9zcGl0YWwub3JnIiwicm9sZXMiOlsiQWRtaW4iXSwicGVybWlzc2lvbnMiOlsiVmlld1N0dWRpZXMiLCJNYW5hZ2VOb2RlcyJdLCJleHAiOjE3MTU2ODAzNjAsImlhdCI6MTcxNTY3Njc2MH0.abc123",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 3600
}
```

| Field | Type | Description |
|-------|------|-------------|
| `accessToken` | string | JWT, valid for `expiresIn` seconds |
| `refreshToken` | string | Opaque token; stored server-side, single-use |
| `expiresIn` | integer | Access token lifetime in seconds (default `3600`) |

### Response `401 Unauthorized` (bad credentials)

```json
{
  "type": "https://edgeguard.io/errors/invalid-credentials",
  "title": "Invalid credentials",
  "status": 401,
  "detail": "The email or password is incorrect.",
  "instance": "/api/auth/login"
}
```

---

## JWT Payload (Decoded)

The access token is a standard JWT. Decode the payload (middle segment, base64url) to inspect claims:

```json
{
  "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Dr. Ana Martínez",
  "email": "ana.martinez@hospital.org",
  "roles": ["Radiologist"],
  "permissions": ["ViewStudies", "ExportData"],
  "iat": 1715676760,
  "exp": 1715680360,
  "iss": "EdgeGuardHub",
  "aud": "EdgeGuardClients"
}
```

| Claim | Description |
|-------|-------------|
| `sub` | User UUID (stable unique identifier) |
| `name` | Display name |
| `email` | Login email |
| `roles` | Array of role names |
| `permissions` | Flat list of granted permissions derived from roles |
| `iat` | Issued-at (Unix epoch) |
| `exp` | Expiration (Unix epoch) |
| `iss` | Token issuer (`EdgeGuardHub`) |
| `aud` | Intended audience (`EdgeGuardClients`) |

> **Security note:** The SPA reads the `permissions` claim to show or hide UI elements. The Hub always re-validates permissions server-side on every request — client-side claim inspection is for UX only.

---

## Refresh Token Flow

When the access token expires the client uses the refresh token to obtain a new pair without re-prompting for credentials.

### Request

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

### Response `200 OK`

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...(new token)...",
  "refreshToken": "bmV3UmVmcmVzaFRva2Vu...(new refresh token)...",
  "expiresIn": 3600
}
```

The old refresh token is immediately invalidated after a successful refresh (token rotation). If the old token is presented again, both old and new tokens are revoked (refresh token reuse detection).

### Response `401 Unauthorized`

```json
{
  "type": "https://edgeguard.io/errors/invalid-refresh-token",
  "title": "Invalid or expired refresh token",
  "status": 401,
  "detail": "The refresh token has expired or been revoked.",
  "instance": "/api/auth/refresh"
}
```

---

## Token Revocation

To explicitly log out, revoke the current refresh token:

```http
POST /api/auth/revoke
Authorization: Bearer <accessToken>
```

**Response `204 No Content`** — The refresh token is invalidated immediately. The access token remains valid until its `exp` claim; clients should discard it locally.

---

## Token Storage Recommendations

| Strategy | Pros | Cons | Recommendation |
|----------|------|------|---------------|
| **In-memory (JS variable)** | Not accessible to XSS scripts | Lost on page refresh | Recommended for access token |
| **sessionStorage** | Survives soft navigation; cleared on tab close | Accessible to XSS | Acceptable for access token |
| **localStorage** | Survives tab close | Accessible to XSS | Avoid for tokens |
| **HttpOnly cookie** | XSS-proof; automatic expiry | Requires CSRF mitigation (`SameSite=Strict`) | Recommended for refresh token |

The EdgeGuard SPA follows this pattern:
- Access token: stored in memory (`AuthService` signal), never written to storage.
- Refresh token: stored in an `HttpOnly`, `SameSite=Strict` cookie set by the Hub.
- On page load the SPA calls `/api/auth/refresh` using the cookie; if it succeeds, the session is restored silently.

---

## Bootstrap Token Flow

On first deployment the Hub database contains no users. A one-time **bootstrap token** is generated at startup and written to the application log. It is used to create the first administrator account.

### Step 1 — Locate the Bootstrap Token

On first startup, search the Hub application log for the token:

```
[WARN] Bootstrap mode active. No admin users found.
       Bootstrap token: BOOTSTRAP-A3F9-7C24-B810
       This token is single-use and will expire in 24 hours.
```

The token is also available in the `BootstrapToken` column of the `SystemSettings` table (cleared after first use).

### Step 2 — Call the Bootstrap Endpoint

```http
POST /api/auth/bootstrap
Content-Type: application/json

{
  "bootstrapToken": "BOOTSTRAP-A3F9-7C24-B810",
  "name": "System Administrator",
  "email": "admin@hospital.org",
  "password": "InitialP@ss1!"
}
```

**Response `201 Created`:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "System Administrator",
  "email": "admin@hospital.org",
  "role": "Admin"
}
```

**Important:** The bootstrap token is single-use. After the first admin is created, this endpoint returns `410 Gone` for all subsequent requests.

---

## Role-Based Access Matrix

EdgeGuard defines a fixed set of roles. Permissions are additive within each role.

| Permission | Admin | Operator | Radiologist | ReadOnly |
|------------|:-----:|:--------:|:-----------:|:--------:|
| `ViewStudies` | Yes | Yes | Yes | Yes |
| `ExportData` | Yes | Yes | Yes | No |
| `ManagePatients` | Yes | Yes | No | No |
| `ManageNodes` | Yes | Yes | No | No |
| `ManagePacs` | Yes | Yes | No | No |
| `ManageRoutingRules` | Yes | Yes | No | No |
| `ManageHl7Rules` | Yes | Yes | No | No |
| `ManageUsers` | Yes | No | No | No |
| `ManageSystemSettings` | Yes | No | No | No |
| `ViewAuditLogs` | Yes | No | No | No |
| `RetryQueueJobs` | Yes | Yes | No | No |

### Role Descriptions

| Role | Description |
|------|-------------|
| **Admin** | Full system access; manages users, settings, and all operational resources |
| **Operator** | Day-to-day operations: nodes, PACS, routing, HL7; cannot manage users or system settings |
| **Radiologist** | Can view and export studies; read-only access to most resources |
| **ReadOnly** | View-only access to studies; suitable for auditing or monitoring users |

Role assignment is performed by an Admin via `PUT /api/users/{id}/role`.
