# Hub Deployment Guide

EdgeGuard Hub is designed to run on **Windows Server with IIS** as its primary deployment target. This guide covers the production IIS deployment plus optional alternatives (Docker, Kestrel + reverse proxy on Linux) for non-standard scenarios.

> **Target environment:** Windows Server 2019 / 2022 + IIS 10 + ASP.NET Core Hosting Bundle + PostgreSQL 16.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [IIS Deployment (Production)](#iis-deployment-production)
3. [HL7 MLLP Listener Considerations](#hl7-mllp-listener-considerations)
4. [Environment Variables Reference](#environment-variables-reference)
5. [First-Run Bootstrap](#first-run-bootstrap)
6. [Database Migrations (Auto-applied)](#database-migrations-auto-applied)
7. [Production Checklist](#production-checklist)
8. [Optional — Docker Compose](#optional--docker-compose)
9. [Optional — Linux + Kestrel + Nginx](#optional--linux--kestrel--nginx)

---

## Prerequisites

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| Windows Server | 2019 | 2022 |
| IIS | 10.0 | 10.0 |
| ASP.NET Core Hosting Bundle | 10.0 | 10.0 (latest patch) |
| .NET Runtime | 10.0 | 10.0 (latest patch) |
| PostgreSQL | 15 | 16 |
| RAM | 4 GB | 8 GB |
| CPU | 2 cores | 4 cores |
| Disk (logs + temp) | 50 GB | 200 GB |

### Windows Server roles & features

```powershell
# Install IIS with required modules
Install-WindowsFeature -Name Web-Server, Web-WebSockets, Web-Http-Logging, Web-Stat-Compression, Web-Dyn-Compression -IncludeManagementTools
```

### ASP.NET Core Hosting Bundle

Download and install from <https://dotnet.microsoft.com/download/dotnet/10.0>. The bundle installs:

- `AspNetCoreModuleV2` for IIS (the in-process / out-of-process host)
- .NET runtime
- Targeting pack

After installing, restart IIS:

```powershell
net stop was /y
net start w3svc
```

> **Note:** The Hub does NOT store permanent DICOM pixel data — pixel data lives on Edge Nodes. Hub disk is for structured logs, audit trail, and temporary HL7/notification queues.

---

## IIS Deployment (Production)

### 1. Publish the application

From a build machine with the .NET SDK:

```powershell
dotnet publish src\backend\Dicom.Edge.Hub.Api `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\inetpub\EdgeGuard\Hub
```

> Recommended path: `C:\inetpub\EdgeGuard\Hub`. The application pool identity needs **Read/Execute** on this folder and **Modify** on `logs\` and `data\`.

### 2. Create the Application Pool

```powershell
Import-Module WebAdministration

# Application pool with No Managed Code (ASP.NET Core runs out-of-process via AspNetCoreModuleV2)
New-WebAppPool -Name "EdgeGuardHub"
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name startMode             -Value AlwaysRunning
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name processModel.idleTimeout -Value "00:00:00"   # never recycle on idle

# Run under a dedicated service account (recommended) — otherwise use ApplicationPoolIdentity
# Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name processModel -Value @{
#     identityType   = "SpecificUser"
#     userName       = "DOMAIN\edgeguard_hub_svc"
#     password       = "<secure password>"
# }
```

### 3. Create the Web Site

```powershell
New-WebSite -Name "EdgeGuard.Hub" `
            -PhysicalPath "C:\inetpub\EdgeGuard\Hub" `
            -ApplicationPool "EdgeGuardHub" `
            -Port 443 `
            -HostHeader "hub.your-org.local" `
            -Ssl

# Bind your TLS certificate (e.g. from Active Directory CS or your enterprise CA)
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -like "*hub.your-org.local*"
New-Item -Path "IIS:\SslBindings\0.0.0.0!443" -Thumbprint $cert.Thumbprint -SSLFlags 1
```

### 4. `web.config`

The publish output already contains a `web.config`. Verify the AspNetCoreModuleV2 settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*"
             modules="AspNetCoreModuleV2"
             resourceType="Unspecified" />
      </handlers>

      <aspNetCore processPath="dotnet"
                  arguments=".\Dicom.Edge.Hub.Api.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>

      <!-- Increase upload size for CSV imports / DICOM proxy -->
      <security>
        <requestFiltering>
          <requestLimits maxAllowedContentLength="536870912" />  <!-- 512 MB -->
        </requestFiltering>
      </security>
    </system.webServer>
  </location>
</configuration>
```

> **`hostingModel`** — use `InProcess` for best performance (the .NET runtime runs inside `w3wp.exe`). Use `OutOfProcess` only if you need isolation between IIS and the .NET process.

### 5. Configure connection string and secrets

Configuration precedence (highest first):

1. Environment variables on the App Pool
2. `appsettings.Production.json` next to the binary
3. `appsettings.json`

**Option A — Environment variables on the App Pool (recommended for secrets):**

```powershell
$pool = "IIS:\AppPools\EdgeGuardHub"
Set-ItemProperty $pool -Name "environmentVariables" -Value @(
    @{ name = "HUB_DB_CONNECTION_STRING"; value = "Host=db.your-org.local;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=...;SSL Mode=Require" },
    @{ name = "Jwt__Secret";    value = "<at-least-32-char-random-secret>" },
    @{ name = "Jwt__Issuer";    value = "https://hub.your-org.local" },
    @{ name = "Jwt__Audience";  value = "edgeguard-clients" }
)
```

**Option B — `appsettings.Production.json`** (safer for non-secret settings):

```jsonc
{
  "ConnectionStrings": {
    "HubDatabase": "Host=db.your-org.local;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=secret;SSL Mode=Require"
  },
  "Diagnostics": {
    "Redaction": { "Mode": "Strict" },
    "Seq":       { "Enabled": false }
  },
  "Cors": {
    "AllowedOrigins": [ "https://hub.your-org.local" ]
  },
  "NodeAuth": { "Enforce": false },   // P0-1: enable after coordinated Hub+Node rollout
  "Hl7Listener": { "ValidateBeforeAck": false }  // P0-5: enable after monitoring
}
```

### 6. WebSocket support for SignalR

WebSockets must be enabled at both the server feature level (`Install-WindowsFeature Web-WebSockets`) and the site level. Verify in **IIS Manager → site → Configuration Editor → `system.webServer/webSocket`**:

```xml
<webSocket enabled="true" />
```

If using ARR or another reverse proxy in front of IIS, ensure `Upgrade` / `Connection` headers are forwarded.

### 7. Start and verify

```powershell
Start-WebSite -Name "EdgeGuard.Hub"

# Health check
Invoke-RestMethod https://hub.your-org.local/health

# Tail the application log
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Wait -Tail 50
```

### 8. Folder permissions

| Path | App pool identity needs |
|------|-------------------------|
| `C:\inetpub\EdgeGuard\Hub` | Read & Execute |
| `C:\inetpub\EdgeGuard\Hub\logs` | Modify |
| `C:\inetpub\EdgeGuard\Hub\data` | Modify |

```powershell
$acl  = Get-Acl "C:\inetpub\EdgeGuard\Hub\logs"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS AppPool\EdgeGuardHub", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl  "C:\inetpub\EdgeGuard\Hub\logs" $acl
```

---

## HL7 MLLP Listener Considerations

The HL7 MLLP TCP listener on port `8001` runs **inside the Hub process**. Under IIS in-process hosting, this means:

| Concern | Mitigation |
|---------|------------|
| App pool recycles drop active MLLP connections | Disable idle timeout (`processModel.idleTimeout = "00:00:00"`), set `startMode = AlwaysRunning`, schedule recycles for low-traffic windows |
| App pool start-on-first-request delays MLLP availability | Use **Application Initialization** feature + `<applicationInitialization>` in `web.config` to warm up on app pool start |
| IIS only routes HTTP — port 8001 is direct TCP, not via IIS | Open port 8001 in Windows Firewall scoped to your HIS/RIS network. Do NOT publish it through ARR or HTTPS — it is a raw TCP socket |

### Application Initialization snippet

```xml
<system.webServer>
  <applicationInitialization doAppInitAfterRestart="true">
    <add initializationPage="/health" />
  </applicationInitialization>
</system.webServer>
```

```powershell
Set-ItemProperty IIS:\Sites\EdgeGuard.Hub -Name applicationDefaults.preloadEnabled -Value $true
```

### Firewall — HL7 MLLP

```powershell
New-NetFirewallRule `
  -DisplayName "EdgeGuard Hub - HL7 MLLP" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 8001 `
  -RemoteAddress 10.20.30.0/24 `  # HIS/RIS subnet — restrict to known hosts
  -Action Allow `
  -Profile Domain
```

---

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `HUB_DB_CONNECTION_STRING` | Yes | — | PostgreSQL connection string |
| `ASPNETCORE_ENVIRONMENT` | Yes | `Production` | ASP.NET Core environment name |
| `Jwt__Secret` | Yes | — | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | Yes | — | JWT issuer claim |
| `Jwt__Audience` | Yes | — | JWT audience claim |
| `Jwt__ExpiryMinutes` | No | `60` | Access token expiry |
| `Cors__AllowedOrigins__0` | Yes | — | First allowed CORS origin (index-based) |
| `Diagnostics__Redaction__Mode` | No | `Relaxed` | `Strict` (PHI redacted) or `Relaxed` |
| `Diagnostics__Seq__Enabled` | No | `false` | Enable Seq log sink |
| `Hl7Listener__Enabled` | No | `true` | Enable the MLLP listener |
| `Hl7Listener__Port` | No | `8001` | MLLP listen port |
| `Hl7Listener__ValidateBeforeAck` | No | `false` | **P0-5** — validate before ACK; enable after monitoring NACK rates |
| `NodeAuth__Enforce` | No | `false` | **P0-1** — set to `true` once all Nodes have rolled out auth headers |

---

## First-Run Bootstrap

On first startup with an empty database, the Hub generates a bootstrap admin token and logs it to stdout / the rolling log file:

```
[INF] ========================================================
[INF] BOOTSTRAP TOKEN (one-time use):
[INF] eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] Use this token to create the initial admin account.
[INF] ========================================================
```

Steps:

1. Copy the token from `C:\inetpub\EdgeGuard\Hub\logs\hub-<date>.log`.
2. POST to `/api/auth/bootstrap`:

   ```powershell
   $body = @{
       username = "admin"
       password = "StrongPassword123!"
       email    = "admin@your-org.local"
   } | ConvertTo-Json

   Invoke-RestMethod -Method Post `
     -Uri https://hub.your-org.local/api/auth/bootstrap `
     -Headers @{ Authorization = "Bearer <bootstrap_token>" } `
     -ContentType "application/json" `
     -Body $body
   ```

3. The bootstrap token is invalidated after first use. Log in normally with the created credentials.

> **Warning:** The bootstrap token appears **only once**. If missed, drop and recreate the database (the Hub re-seeds on next start) or run the manual admin-seed script (`scripts/seed-admin.sql`).

---

## Database Migrations (Auto-applied)

The Hub applies pending EF Core migrations at startup via `app.Services.MigrateHubAsync()`. **No manual migration step is required for standard deployments.** Migration output appears in the rolling log:

```
[INF] Applying database migrations...
[INF] Applied 'AddNodeDicomRoutingRules'
[INF] Applied 'AddHl7MrgObxSupport'
[INF] All migrations applied successfully.
```

For controlled production deployments (e.g. ITSM-gated change windows), see [database-migrations.md](./database-migrations.md) for manual `dotnet ef database update` instructions.

---

## Production Checklist

- [ ] `ASPNETCORE_ENVIRONMENT=Production` set on the App Pool
- [ ] App pool `startMode=AlwaysRunning`, `idleTimeout=00:00:00`
- [ ] `Application Initialization` configured to preload `/health`
- [ ] TLS certificate bound to port 443 (TLS 1.2+)
- [ ] WebSockets feature installed and enabled at site level
- [ ] `Jwt__Secret` is ≥32 chars, randomly generated, stored as App Pool env var (not in `appsettings.json`)
- [ ] `Cors:AllowedOrigins` restricted to your actual SPA domain
- [ ] `Diagnostics__Redaction__Mode=Strict`
- [ ] PostgreSQL user has `CONNECT`, `CREATE`, schema ownership only (no superuser)
- [ ] PostgreSQL not exposed on public network interface
- [ ] `logs\` folder is writable but not world-readable
- [ ] `/health` returns `Healthy` before adding to the load balancer
- [ ] Backup schedule configured (see [backup-recovery.md](./backup-recovery.md))
- [ ] HL7 MLLP port 8001 firewall-restricted to HIS/RIS subnet
- [ ] Application Insights / Seq / Serilog file sink configured (see [monitoring.md](./monitoring.md))
- [ ] App Pool runs under a dedicated service account (not `LocalSystem`)

---

## Optional — Docker Compose

For non-IIS scenarios (containerized dev environments, Linux hosts), the Hub also runs as a Kestrel-only ASP.NET Core app.

### docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:16-alpine
    container_name: edgeguard-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: edgeguard
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: edgeguard_hub
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U edgeguard -d edgeguard_hub"]
      interval: 10s
      timeout: 5s
      retries: 5

  hub:
    image: edgeguard/hub:latest
    container_name: edgeguard-hub
    restart: unless-stopped
    depends_on:
      postgres: { condition: service_healthy }
    environment:
      HUB_DB_CONNECTION_STRING: "Host=postgres;Database=edgeguard_hub;Username=edgeguard;Password=${POSTGRES_PASSWORD}"
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: "http://+:5000"
      Jwt__Secret: ${JWT_SECRET}
    ports:
      - "5000:5000"
      - "8001:8001"
    volumes:
      - hub_logs:/app/logs
      - hub_data:/app/data
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_data:
  hub_logs:
  hub_data:
```

---

## Optional — Linux + Kestrel + Nginx

The Hub can also be hosted on Linux behind Nginx as a reverse proxy. See `appendix-linux-deployment.md` for the systemd unit and Nginx reverse-proxy template (kept for parity; **not the primary deployment target**).
