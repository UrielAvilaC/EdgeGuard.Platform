# Prerequisites

Review and satisfy every item in this checklist before attempting installation. Items marked **Required** will prevent the system from starting if absent; items marked **Recommended** affect production reliability.

> **Target platform:** Windows Server. The Hub deploys on **IIS** and the Edge Node runs as a **Windows Service**. Linux is supported for development / non-standard scenarios — see deployment guides for details.

---

## Runtime Software

### Hub Server (Windows + IIS)

| Requirement | Version | Status | Notes |
|-------------|---------|--------|-------|
| Windows Server | 2019 / 2022 | **Required** | Production target. Windows 10/11 supported for dev. |
| IIS | 10.0 | **Required** | Install via `Install-WindowsFeature Web-Server, Web-WebSockets, Web-Http-Logging, Web-Stat-Compression, Web-Dyn-Compression -IncludeManagementTools` |
| ASP.NET Core Hosting Bundle | 10.0 | **Required** | Installs the `AspNetCoreModuleV2` for IIS plus the .NET runtime. Download from [dot.net](https://dotnet.microsoft.com/download/dotnet/10.0). |
| .NET SDK | 10.0+ | Required on build machine | Used to publish; not required on the IIS server itself. |
| Node.js | 22.x LTS+ | Required on build machine | Builds the Angular SPA before publish. |
| Angular CLI | 21.x | Required on build machine | `npm install -g @angular/cli@21`. |
| EF Core CLI | 10.x | Required for manual migrations | `dotnet tool install --global dotnet-ef`. Auto-migrations on startup do not need this. |
| Git | 2.x+ | Recommended | Required to clone the repository. |

> **WebSockets** must be enabled in IIS for SignalR real-time notifications. Verify under **IIS Manager → site → Configuration Editor → `system.webServer/webSocket`** (`enabled="true"`).

### Edge Node (Windows Service)

| Requirement | Version | Status | Notes |
|-------------|---------|--------|-------|
| Windows Server | 2019 / 2022 | **Required** | Or Windows 10 / 11 Pro for small sites. |
| .NET Runtime | 10.0+ | **Required** | Install `dotnet-runtime-10.0.x-win-x64.exe` (NOT the SDK and NOT the ASP.NET Hosting Bundle — Node hosts Kestrel directly, no IIS). |
| Administrator | — | **Required for install** | Needed for `sc.exe create` and Windows Firewall rules. |
| .NET SDK | 10.0+ | Required for dev | Only on build machines, not on production nodes. |

> SQLite is bundled with the .NET runtime via `Microsoft.Data.Sqlite` and requires no separate installation.

---

## Database

### Hub — PostgreSQL

| Item | Requirement |
|------|-------------|
| Version | PostgreSQL **16** or later (15 works; 16 recommended) |
| Hosting | Standalone Windows / Linux server, managed service (Azure Database for PostgreSQL, Amazon RDS, Cloud SQL), or Docker container |
| User permissions | The connection-string user needs `CONNECT`, `CREATE`, schema ownership, and `SELECT/INSERT/UPDATE/DELETE` plus `CREATE TABLE / CREATE INDEX` for EF Core migrations |
| Encoding | `UTF8` |
| Collation | `en-US-x-icu` or `C` (any consistent collation works) |
| Network | Reachable from the IIS Hub server on TCP 5432; consider SSL/TLS in transit |

```sql
-- Minimal PostgreSQL setup
CREATE ROLE edgeguard LOGIN PASSWORD '<strong password>';
CREATE DATABASE edgeguard_hub OWNER edgeguard ENCODING 'UTF8';
```

### Edge Node — SQLite

SQLite requires no external installation. The database file is created automatically at `C:\EdgeGuard\Node\persistence\edge-node.db` on first startup. Ensure the service account has Modify on the `persistence\` folder.

---

## Network Ports

The following ports must be open between the indicated hosts. Adjust Windows Firewall rules before installation.

### Hub Server

| Port | Protocol | Direction | Purpose | Required? |
|------|----------|-----------|---------|-----------|
| **443** | TCP | Inbound | HTTPS via IIS (SPA + API + SignalR WebSockets) | **Required** (production) |
| **80** | TCP | Inbound | HTTP → HTTPS redirect via IIS | Recommended |
| **8001** | TCP | Inbound | HL7 MLLP listener (raw TCP, NOT through IIS) | Required if HL7 enabled |
| **5432** | TCP | Outbound | PostgreSQL connection (to DB server) | **Required** |

> The HL7 MLLP listener on `:8001` is a raw TCP socket opened by the Hub process — it does NOT go through IIS. Open the firewall scoped to your HIS/RIS subnet only.

### Edge Node

| Port | Protocol | Direction | Purpose | Required? |
|------|----------|-----------|---------|-----------|
| **11112** | TCP | Inbound | DICOM C-STORE / C-FIND MWL / C-ECHO SCP (from modalities) | **Required** |
| **443** | TCP | Outbound | HTTPS to Hub API (registration, config sync, telemetry) | **Required** |
| **5001** | TCP | Inbound | Node admin/health endpoints (internal-only) | Required for monitoring |
| PACS port | TCP | Outbound | DICOM C-STORE to PACS (commonly 104 or 11112) | Required for forwarding |

### Recommended Windows Firewall Rules

```powershell
# Hub — HL7 MLLP, restricted to HIS/RIS subnet
New-NetFirewallRule -DisplayName "EdgeGuard Hub - HL7 MLLP" `
  -Direction Inbound -Protocol TCP -LocalPort 8001 `
  -RemoteAddress 10.20.30.0/24 -Action Allow -Profile Domain

# Edge Node — DICOM SCP, restricted to modality subnet
New-NetFirewallRule -DisplayName "EdgeGuard Node - DICOM SCP" `
  -Direction Inbound -Protocol TCP -LocalPort 11112 `
  -RemoteAddress 10.10.20.0/24 -Action Allow -Profile Domain,Private

# Edge Node — Admin endpoint, restricted to admin subnet
New-NetFirewallRule -DisplayName "EdgeGuard Node - Admin API" `
  -Direction Inbound -Protocol TCP -LocalPort 5001 `
  -RemoteAddress 10.10.99.0/24 -Action Allow -Profile Domain
```

---

## Minimum Hardware

### Hub Server (Windows + IIS + PostgreSQL co-located or separate)

| Resource | Minimum | Recommended (production) |
|----------|---------|--------------------------|
| CPU cores | 2 | 4+ |
| RAM | 4 GB | 8 GB+ |
| Disk | 50 GB (OS + app + logs) | 200 GB+ (audit + HL7 retention) |
| Disk type | HDD | SSD (PostgreSQL WAL performance) |
| Network | 100 Mbps | 1 Gbps |

> **Note:** The Hub stores only DICOM metadata (UIDs, patient/study records, audit logs, HL7 messages) — NOT pixel data. Pixel data lives on Edge Nodes and the destination PACS. Hub disk is sized for audit + HL7 retention.

### Edge Node (Windows Service)

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU cores | 1 | 2+ |
| RAM | 2 GB | 4 GB |
| Disk | 100 GB | 500 GB+ (pixel data buffered locally until forwarded) |
| Disk type | HDD | SSD (DICOM write throughput, SQLite WAL) |
| Network | 100 Mbps | 1 Gbps (CT/MR series are large) |

> **Note:** Edge Node disk usage is transient. Studies are deleted after successful PACS delivery per the `DicomServer:InstanceRetentionDays` policy. Size for **peak daily study volume × retention days**.

---

## Supported Operating Systems

| OS | Architecture | Hub | Edge Node | Status |
|----|-------------|-----|-----------|--------|
| **Windows Server 2022** | x64 | **Primary** (IIS) | **Primary** (Service) | Production target |
| **Windows Server 2019** | x64 | **Supported** (IIS) | **Supported** (Service) | Production |
| Windows 10 / 11 Pro | x64 | Dev / small sites | Dev / small sites | Dev + small-deployment |
| Ubuntu 22.04 / 24.04 LTS | x64 | Optional (Kestrel + Nginx) | Optional (systemd) | Non-standard scenarios |
| Debian 12 | x64 | Optional | Optional | Non-standard scenarios |
| macOS 14+ | arm64 / x64 | Dev only | Dev only | Not supported for production |
| Docker (Linux container) | x64 | Optional | Optional | Containerized dev / labs |

---

## Optional Dependencies

| Dependency | Purpose | Configuration key |
|------------|---------|-------------------|
| Seq | Centralized structured log viewer | `Diagnostics:Seq:Enabled` |
| OpenTelemetry Collector | Distributed tracing / metrics export | `Diagnostics:OpenTelemetry:Enabled` |
| WhatsApp messaging gateway | Study-received alerts to clinicians | `Notifications:WhatsApp:GatewayUrl` |
| Active Directory CS | Issuing TLS certificates for Hub and DICOM | External (PKI) |
| Application Request Routing (ARR) | Reverse proxy in front of IIS for multi-site | IIS module |

---

## Pre-Installation Checklist

Before running the quickstart guides, confirm:

### Build machine
- [ ] .NET 10 SDK installed (`dotnet --version` ≥ 10.0)
- [ ] Node.js 22+ installed (`node --version` ≥ 22.0)
- [ ] Angular CLI 21 installed (`ng version`)
- [ ] EF Core CLI installed (`dotnet ef --version`)

### Hub host (Windows + IIS)
- [ ] Windows Server 2019/2022 with administrator access
- [ ] IIS + WebSockets + management tools installed
- [ ] ASP.NET Core 10 Hosting Bundle installed
- [ ] TLS certificate available (CA-issued) bound to port 443
- [ ] PostgreSQL 16 reachable; database and user created
- [ ] `HUB_DB_CONNECTION_STRING` ready to set on the App Pool
- [ ] Port 8001 open in Windows Firewall scoped to HIS/RIS subnet

### Edge Node host (Windows Service)
- [ ] Windows Server 2019/2022 (or Win 10/11 Pro) with administrator access
- [ ] .NET 10 Runtime installed (`dotnet --list-runtimes` shows `Microsoft.NETCore.App 10.0.x`)
- [ ] Port 11112 open in Windows Firewall scoped to modality subnet
- [ ] Outbound HTTPS to Hub reachable
- [ ] Service account decided (default `NT AUTHORITY\NetworkService`, or dedicated AD account)
- [ ] Bootstrap token generated in the Hub UI ready to paste into `appsettings.Production.json`
