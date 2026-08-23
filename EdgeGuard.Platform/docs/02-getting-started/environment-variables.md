# Environment Variables

This reference covers every configuration key recognized by EdgeGuard Platform components. Keys can be supplied as:

1. **Environment variables** — highest precedence. Use double-underscore (`__`) as a section separator on Linux/macOS (e.g., `Jwt__SecretKey`). On Windows, both `__` and `:` work.
2. **`appsettings.Production.json`** — merged at startup, overrides `appsettings.json`.
3. **`appsettings.json`** — baseline defaults shipped with the application.

> **Security note:** Never commit secrets (JWT secret, database passwords, API keys) to source control. Use environment variables, a secrets manager (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault), or the .NET Secret Manager (`dotnet user-secrets`) for local development.

> **Setting env vars on IIS:** On the Hub, environment variables go on the **App Pool** (IIS Manager → Application Pools → EdgeGuardHub → Advanced Settings → Environment Variables). Avoid putting secrets in `appsettings.json`.
>
> **Setting env vars on a Windows Service:** For the Node, use `appsettings.Production.json` next to the service binary, OR set machine-wide env vars via `[Environment]::SetEnvironmentVariable("KEY", "value", "Machine")` and restart the service (`Restart-Service EdgeGuardNode`).

---

## Hub Configuration

### Database

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `EDGEGUARD_HUB_CONNECTIONSTRING` | *(none)* | **Yes** | PostgreSQL connection string. Environment variable form; takes highest precedence over all appsettings values. Example: `Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=secret` |
| `ConnectionStrings:HubDatabase` | *(none)* | Fallback | Fallback PostgreSQL connection string in appsettings. Used when `EDGEGUARD_HUB_CONNECTIONSTRING` is not set. **Deprecated** — prefer the environment variable form. |

### HL7 Listener

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Hl7Listener:Enabled` | `true` | No | Set to `false` to disable the MLLP TCP listener entirely. Useful when HL7 integration is not needed or when a separate service handles HL7. |
| `Hl7Listener:Port` | `8001` | No | TCP port for the MLLP listener. Change if port 8001 is occupied or restricted by policy. Must be updated in the HIS/RIS HL7 sender configuration to match. |
| `Hl7Listener:MaxConcurrentConnections` | `10` | No | Maximum number of simultaneous MLLP connections accepted. Excess connections are queued at the TCP layer. |
| `Hl7Listener:ReceiveTimeoutSeconds` | `30` | No | Seconds to wait for a complete HL7 message after accepting a connection before closing it. Prevents hung connections from exhausting the connection pool. |

### CORS

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` | No | JSON array of allowed origins for CORS requests. In production, set this to the exact URL(s) from which the Angular SPA is served. Example: `["https://edgeguard.hospital.internal"]`. Wildcard (`*`) is not supported for credentialed requests. |
| `Cors:AllowCredentials` | `true` | No | Whether to include `Access-Control-Allow-Credentials: true` in CORS responses. Must be `true` for JWT cookie authentication; leave `true` for Bearer-token flows as well. |

### Diagnostics and Logging

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Diagnostics:InstanceId` | `HUB-DEV` | No | Human-readable identifier included as a structured property (`InstanceId`) in every log entry. Set to a meaningful value in production (e.g., `HUB-PROD-01`). |
| `Diagnostics:Redaction:Mode` | `Relaxed` | No | PHI redaction level for log output. `Relaxed` — redacts patient name and date of birth but retains AccessionNumber, StudyInstanceUID, and PatientID for operational debugging. `Strict` — redacts all patient-identifiable fields including AccessionNumber. |
| `Serilog:MinimumLevel:Default` | `Information` | No | Default Serilog minimum log level. Valid values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. |
| `Seq:ServerUrl` | *(none)* | No | URL of a Seq log server (e.g., `http://seq.internal:5341`). When set, the Serilog Seq sink is activated and all structured log events are forwarded. |
| `Seq:ApiKey` | *(none)* | No | Seq API key for authenticated ingestion. |
| `OpenTelemetry:Enabled` | `false` | No | Set to `true` to activate OTLP trace and metric export. |
| `OpenTelemetry:Endpoint` | *(none)* | No | OTLP gRPC endpoint for traces and metrics (e.g., `http://otel-collector:4317`). Required when `OpenTelemetry:Enabled=true`. |

### JWT Authentication

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Jwt:Secret` | `dev-secret-change-in-production` | **Yes (prod)** | HMAC-SHA256 signing key for JWT access tokens. Must be at least 32 characters. **Change this before any production deployment.** A cryptographically random 64-character string is recommended. |
| `Jwt:Issuer` | `EdgeGuard.Hub` | No | The `iss` claim value embedded in issued tokens. Validated on every token verification. If you run multiple Hub instances, all must share the same issuer value. |
| `Jwt:Audience` | `EdgeGuard.Clients` | No | The `aud` claim value embedded in issued tokens. Must match the audience configured in consuming clients. |
| `Jwt:ExpiryMinutes` | `60` | No | Lifetime of an access token in minutes. After expiry, the client must use its refresh token to obtain a new access token. Shorter values reduce the window of exposure if a token is leaked. |
| `Jwt:RefreshTokenExpiryDays` | `30` | No | Lifetime of a refresh token in days. After expiry, the user must log in again with their credentials. |

### Node Registration

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `NodeRegistration:Secret` | *(none)* | No | Pre-shared secret that Edge Nodes must present in the `X-Node-Registration-Secret` header during first-boot registration. When set, only nodes that know this secret can register. Leave unset in development to allow open registration. |
| `NodeRegistration:AutoApprove` | `true` | No | When `true`, newly registered nodes are immediately approved and receive a node JWT. When `false`, an administrator must manually approve each node in the SPA before it can communicate with the Hub. |

### Rate Limiting

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `RateLimiting:EdgePolicy:PermitLimit` | `100` | No | Maximum requests per minute under the `edge` rate limiting policy, applied to node-to-Hub API calls. |
| `RateLimiting:ApiPolicy:PermitLimit` | `200` | No | Maximum requests per minute under the `api` rate limiting policy, applied to SPA and external API clients. |

### Notifications

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Notifications:WhatsApp:Enabled` | `false` | No | Enable WhatsApp alert delivery via the configured gateway. |
| `Notifications:WhatsApp:GatewayUrl` | *(none)* | No | Base URL of the WhatsApp messaging gateway API. |
| `Notifications:WhatsApp:ApiKey` | *(none)* | No | API key for the WhatsApp gateway. |
| `Notifications:Email:SmtpHost` | *(none)* | No | SMTP server hostname for email alert delivery. |
| `Notifications:Email:SmtpPort` | `587` | No | SMTP server port. |
| `Notifications:Email:FromAddress` | *(none)* | No | Sender address for outbound alert emails. |

---

## Edge Node Configuration

### Database

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `ConnectionStrings:NodeDatabase` | `./persistence/edge-node.db` | No | SQLite database file path. Relative paths are resolved from the working directory of the Edge Node process. Use an absolute path in production to avoid ambiguity (e.g., `/var/lib/edgeguard/node/edge-node.db` or `C:\EdgeGuard\Node\persistence\edge-node.db`). |

### Hub Connectivity

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Hub:BaseUrl` | `http://localhost:5000` | **Yes** | Full base URL of the Hub API. Use `https://` in production. Do not include a trailing slash. |
| `Hub:NodeId` | `EDGE-DEFAULT` | **Yes** | Unique identifier for this node instance. Must match the Node ID configured in the Hub UI. Changing this value after registration causes the node to register again as a new node. |
| `Hub:RegistrationSecret` | *(none)* | No | Pre-shared secret to present during registration, matching `NodeRegistration:Secret` on the Hub. Required when the Hub has open registration disabled. |
| `Hub:HeartbeatIntervalSeconds` | `30` | No | Interval at which the node sends a heartbeat POST to the Hub to indicate it is alive. The Hub marks a node `Unreachable` if no heartbeat is received within 3× this interval. |
| `Hub:PushRetryMaxAttempts` | `5` | No | Maximum number of retry attempts when the Hub fails to push a config update to this node. |

### DICOM Server

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `DicomServer:AeTitle` | `EDGENODE` | **Yes** | AE Title advertised by this node's DICOM SCP. Must be configured identically on all modalities and PACS servers that connect to this node. Max 16 characters, uppercase, no spaces. |
| `DicomServer:Port` | `11112` | No | TCP port for the DICOM SCP (C-STORE, C-FIND, C-ECHO). The standard DICOM port is 11112; some institutions use 104 (requires elevated privileges on Linux). |
| `DicomServer:MaxConcurrentAssociations` | `10` | No | Maximum number of simultaneous DICOM associations handled. Each association uses one thread. |
| `DicomServer:AllowedCallingAeTitles` | `[]` | No | JSON array of AE Titles allowed to open associations to this node. When empty (default), any calling AE Title is accepted. Populate this list to restrict access to known modalities and PACS. Example: `["CT_ROOM_1","PACS_MAIN"]`. |
| `DicomServer:InstanceRetentionDays` | `7` | No | Number of days to retain delivered DICOM instance files in SQLite before automatic deletion. Set to `0` to delete immediately after confirmed delivery. Set to `-1` to disable automatic deletion. |
| `DicomServer:StoragePath` | `./dicom-store` | No | Directory where received DICOM instance files are written before forwarding. Defaults to a subdirectory of the working directory. Use a dedicated volume in production. |

### Diagnostics and Logging

| Key | Default | Required | Description |
|-----|---------|----------|-------------|
| `Diagnostics:InstanceId` | `NODE-001` | No | Human-readable identifier for this node instance, included as a structured property in every log entry. Set to match the `Hub:NodeId` for easy log correlation (e.g., `EDGE-SITE-01`). |
| `Diagnostics:Redaction:Mode` | `Strict` | No | PHI redaction level. Edge Nodes default to `Strict`, which redacts all patient-identifiable fields from logs, including AccessionNumber, PatientName, PatientID, and BirthDate. Do not change to `Relaxed` without a compliance review. |
| `Serilog:MinimumLevel:Default` | `Information` | No | Default Serilog minimum log level for the Edge Node. |
| `Seq:ServerUrl` | *(none)* | No | Optional Seq server URL for centralized log aggregation. |

---

## Example: Minimal Production Configuration

### Hub — environment variables

**PowerShell (Windows + IIS — primary).** Set per-app-pool so values stay scoped to the Hub worker process:

```powershell
Import-Module WebAdministration
$pool = "EdgeGuardHub"
$vars = @{
    "EDGEGUARD_HUB_CONNECTIONSTRING"   = "Host=pg.internal;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=<redacted>"
    "Jwt__SecretKey"                = "<64-character-random-string>"
    "Diagnostics__InstanceId"    = "HUB-PROD-01"
    "Cors__AllowedOrigins__0"    = "https://edgeguard.hospital.internal"
    "NodeRegistration__Secret"   = "<shared-node-secret>"
}
foreach ($kv in $vars.GetEnumerator()) {
    Add-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
        -Filter "system.applicationHost/applicationPools/add[@name='$pool']/environmentVariables" `
        -Name "." -Value @{ name = $kv.Key; value = $kv.Value }
}
Restart-WebAppPool -Name $pool
```

**Bash (Linux, alternative):**

```bash
export EDGEGUARD_HUB_CONNECTIONSTRING="Host=pg.internal;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=<redacted>"
export Jwt__SecretKey="<64-character-random-string>"
export Diagnostics__InstanceId="HUB-PROD-01"
export Cors__AllowedOrigins__0="https://edgeguard.hospital.internal"
export NodeRegistration__Secret="<shared-node-secret>"
```

### Hub — `appsettings.Production.json`

```json
{
  "Hl7Listener": {
    "Enabled": true,
    "Port": 8001
  },
  "Jwt": {
    "Issuer": "EdgeGuard.Hub",
    "Audience": "EdgeGuard.Clients",
    "ExpiryMinutes": 30,
    "RefreshTokenExpiryDays": 7
  },
  "Diagnostics": {
    "Redaction": {
      "Mode": "Relaxed"
    }
  },
  "Seq": {
    "ServerUrl": "http://seq.internal:5341"
  },
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://otel-collector.internal:4317"
  }
}
```

### Edge Node — `appsettings.Production.json` (Windows Service — primary)

Place next to the service binary at `C:\EdgeGuard\Node\appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "NodeDatabase": "C:\\EdgeGuard\\Node\\persistence\\edge-node.db"
  },
  "Hub": {
    "BaseUrl": "https://edgeguard.hospital.internal",
    "NodeId": "EDGE-RADIOLOGY-FLOOR2",
    "RegistrationSecret": "<shared-node-secret>",
    "HeartbeatIntervalSeconds": 30
  },
  "DicomServer": {
    "AeTitle": "EDGE_RAD_F2",
    "Port": 11112,
    "AllowedCallingAeTitles": ["CT_SUITE_2", "MR_SUITE_1", "PACS_MAIN"],
    "InstanceRetentionDays": 3,
    "StoragePath": "C:\\EdgeGuard\\Node\\dicom-store"
  },
  "Diagnostics": {
    "InstanceId": "EDGE-RADIOLOGY-FLOOR2",
    "Redaction": {
      "Mode": "Strict"
    }
  }
}
```

After editing, restart the service:

```powershell
Restart-Service EdgeGuardNode
```

**Linux equivalent (alternative)** — use POSIX paths (`/var/lib/edgeguard/node/edge-node.db`, `/data/dicom-store`) and reload via `sudo systemctl restart edgeguard-node`.
