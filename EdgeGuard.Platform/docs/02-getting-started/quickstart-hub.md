# Quickstart — Hub Central

This guide walks you from a fresh clone to a Hub running on **IIS** in under 30 minutes. For dev-only Kestrel runs (no IIS), see the "Development Run" section at the end.

> **Production target:** Windows Server + IIS + PostgreSQL. See [prerequisites.md](prerequisites.md) for the full requirement list.

---

## Before You Begin

Verify your build machine:

```powershell
dotnet --version    # 10.x.x
node --version      # v22.x or later
ng version          # Angular CLI 21.x
dotnet ef --version # EF Core CLI 10.x
```

Verify your Hub server (Windows + IIS):

```powershell
# IIS installed
Get-WindowsFeature Web-Server, Web-WebSockets | Format-Table Name, InstallState

# ASP.NET Core Hosting Bundle installed (creates AspNetCoreModuleV2 in IIS)
Get-WebGlobalModule -Name "AspNetCoreModuleV2"
```

Ensure PostgreSQL 16+ is running and you have a database ready:

```sql
-- Run as a PostgreSQL superuser
CREATE ROLE edgeguard LOGIN PASSWORD '<strong-password>';
CREATE DATABASE edgeguard_hub OWNER edgeguard ENCODING 'UTF8';
```

---

## Step 1 — Clone and Publish

On the build machine:

```powershell
git clone https://github.com/your-org/EdgeGuard.Platform.git
cd EdgeGuard.Platform\EdgeGuard.Platform

# Build the Angular SPA (output is auto-copied to Hub.Api/wwwroot)
cd src\frontend\dicomedge-ui
npm install
ng build --configuration production
cd ..\..\..

# Publish the Hub for IIS (framework-dependent — uses the Hosting Bundle's runtime)
dotnet publish src\backend\Dicom.Edge.Hub.Api `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\publish\EdgeGuard.Hub
```

The publish folder is now ready to deploy to IIS.

> **Troubleshooting — SPA blank page later:** if `wwwroot` is missing or stale, re-run `ng build --configuration production` before `dotnet publish`. The Angular build step copies its output to `src\backend\Dicom.Edge.Hub.Api\wwwroot\` via the build pipeline.

---

## Step 2 — Deploy to IIS

Copy or robocopy the publish output to the IIS server (or run publish directly on the server):

```powershell
robocopy C:\publish\EdgeGuard.Hub  C:\inetpub\EdgeGuard\Hub /MIR
```

Create the App Pool and Site:

```powershell
Import-Module WebAdministration

# App Pool — No Managed Code (ASP.NET Core runs via AspNetCoreModuleV2)
New-WebAppPool -Name "EdgeGuardHub"
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name managedRuntimeVersion          -Value ""
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name startMode                      -Value AlwaysRunning
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name processModel.idleTimeout       -Value "00:00:00"

# Web site bound to HTTPS on port 443 (replace host header and cert thumbprint)
New-WebSite -Name "EdgeGuard.Hub" `
            -PhysicalPath "C:\inetpub\EdgeGuard\Hub" `
            -ApplicationPool "EdgeGuardHub" `
            -Port 443 `
            -HostHeader "hub.your-org.local" `
            -Ssl

$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -like "*hub.your-org.local*" | Select-Object -First 1
New-Item -Path "IIS:\SslBindings\0.0.0.0!443" -Thumbprint $cert.Thumbprint -SSLFlags 1
```

Grant the App Pool identity write access to `logs\` and `data\`:

```powershell
icacls C:\inetpub\EdgeGuard\Hub\logs /grant "IIS AppPool\EdgeGuardHub:(OI)(CI)M" /T
icacls C:\inetpub\EdgeGuard\Hub\data /grant "IIS AppPool\EdgeGuardHub:(OI)(CI)M" /T
```

---

## Step 3 — Configure the Database Connection String

Set the connection string as an environment variable **on the App Pool** so it isn't checked into source control:

```powershell
$pool = "IIS:\AppPools\EdgeGuardHub"
Set-ItemProperty $pool -Name "environmentVariables" -Value @(
    @{ name = "HUB_DB_CONNECTION_STRING"
       value = "Host=db.your-org.local;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=<strong-password>;SSL Mode=Require" }
    @{ name = "Jwt__Secret"
       value = "<at-least-32-char-random-secret>" }
    @{ name = "Jwt__Issuer"
       value = "https://hub.your-org.local" }
    @{ name = "Jwt__Audience"
       value = "edgeguard-clients" }
    @{ name = "Diagnostics__Redaction__Mode"
       value = "Strict" }
)
```

> **Troubleshooting:** if the Hub fails on first request with `Connection refused`, verify:
> - PostgreSQL is reachable: `Test-NetConnection db.your-org.local -Port 5432`
> - The user/password are correct: `psql -h db.your-org.local -U edgeguard -d edgeguard_hub -c "SELECT 1"`
> - PostgreSQL `pg_hba.conf` allows the IIS host

---

## Step 4 — Open Firewall Ports

```powershell
# HL7 MLLP — raw TCP, scoped to HIS/RIS subnet only
New-NetFirewallRule -DisplayName "EdgeGuard Hub - HL7 MLLP" `
  -Direction Inbound -Protocol TCP -LocalPort 8001 `
  -RemoteAddress 10.20.30.0/24 -Action Allow -Profile Domain

# HTTPS (443) is normally already open if IIS is publicly bound
```

> **Why not through IIS for port 8001?** The HL7 listener is a raw TCP socket inside the Hub process, not an HTTP endpoint. IIS doesn't route it.

---

## Step 5 — Start the Site and Get the Bootstrap Token

```powershell
Start-WebSite -Name "EdgeGuard.Hub"
```

The Hub auto-applies pending EF Core migrations on first start. On an empty database, it generates a **bootstrap token** and writes it to the rolling log:

```
[INF] ========================================================
[INF] BOOTSTRAP TOKEN (one-time use):
[INF] eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] ========================================================
```

```powershell
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Wait -Tail 50
```

> **The bootstrap token is shown only once.** Copy it now.

> **Troubleshooting — site doesn't start:** check the Windows Event Log under **Application** for `AspNetCoreModule` errors. Common causes:
> - Hosting Bundle not installed → install and `iisreset`
> - App Pool identity has no write access to `logs\` → re-run the `icacls` command
> - Connection string wrong → check App Pool env vars under **IIS Manager → App Pool → Advanced Settings → Environment Variables**

---

## Step 6 — Create the First Administrator

Open `https://hub.your-org.local/` in a browser. The SPA loads and presents the **Bootstrap Login** screen.

Or via PowerShell:

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

The bootstrap token is invalidated. Log in normally at `https://hub.your-org.local/login` with the new admin credentials.

**Next steps from the dashboard:**

1. **Nodes → Register New Node** — generate a bootstrap token for your first Edge Node ([quickstart-node.md](quickstart-node.md)).
2. **PACS Destinations** — add the target PACS servers.
3. **Routing Rules** — configure which studies go to which PACS.

---

## Production Hardening Checklist

- [ ] App Pool `startMode=AlwaysRunning`, `idleTimeout=00:00:00`
- [ ] Application Initialization warms up `/health` on app pool start
- [ ] TLS certificate bound to port 443 (TLS 1.2+); HTTP→HTTPS redirect rule
- [ ] WebSockets enabled at site level (SignalR)
- [ ] `Jwt__Secret` is ≥32 chars random, stored in App Pool env vars (not in `appsettings.json`)
- [ ] `Diagnostics__Redaction__Mode=Strict`
- [ ] `Cors:AllowedOrigins` restricted to your SPA URL
- [ ] PostgreSQL user has minimal privileges (not superuser)
- [ ] HL7 MLLP firewall scoped to HIS/RIS subnet
- [ ] Backup schedule configured ([backup-recovery.md](../07-operations/backup-recovery.md))

Full deployment guide: [deployment-hub.md](../07-operations/deployment-hub.md).

---

## Development Run (No IIS)

For local development against the source tree (no IIS), run Kestrel directly:

```powershell
$env:HUB_DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=..."
dotnet run --project src/backend/Dicom.Edge.Hub.Api
# Listening on http://localhost:5000 / https://localhost:5001
```

Open `http://localhost:5000` to access the SPA. The Angular dev server (`ng serve`) is a separate workflow that proxies to this Kestrel instance — see `src/frontend/dicomedge-ui/README.md`.

---

## Summary

| Step | Action |
|------|--------|
| 1 | Clone, `npm install`, `ng build`, `dotnet publish` |
| 2 | Deploy to `C:\inetpub\EdgeGuard\Hub`; create App Pool + Site |
| 3 | Set connection string + JWT secret as App Pool env vars |
| 4 | Open port 8001 in Windows Firewall (HIS/RIS subnet) |
| 5 | Start site; copy bootstrap token from `logs\hub-<date>.log` |
| 6 | Create first administrator via SPA or `POST /api/auth/bootstrap` |
