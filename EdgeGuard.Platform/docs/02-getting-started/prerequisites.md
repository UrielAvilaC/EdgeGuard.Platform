# Prerequisites

Review and satisfy every item in this checklist before attempting installation. Items marked **Required** will prevent the system from starting if absent; items marked **Recommended** affect production reliability.

---

## Runtime Software

### Hub Server

| Requirement | Version | Status | Notes |
|-------------|---------|--------|-------|
| .NET SDK | 10.0+ | **Required** | Install from [dot.net](https://dot.net). Verify with `dotnet --version`. |
| Node.js | 22.x LTS+ | **Required** | Needed to build the Angular SPA. Verify with `node --version`. |
| Angular CLI | 21.x | **Required** | Install globally: `npm install -g @angular/cli@21`. Verify with `ng version`. |
| Git | 2.x+ | Recommended | Required to clone the repository. |
| EF Core CLI | 10.x | **Required** | `dotnet tool install --global dotnet-ef`. Verify with `dotnet ef --version`. |

### Edge Node

| Requirement | Version | Status | Notes |
|-------------|---------|--------|-------|
| .NET Runtime | 10.0+ | **Required** | SDK not required on production nodes — runtime is sufficient. |
| .NET SDK | 10.0+ | Required for dev | Needed if building from source on the node. |

> **Note:** SQLite is bundled with the .NET runtime via Microsoft.Data.Sqlite and requires no separate installation.

---

## Database

### Hub — PostgreSQL

| Item | Requirement |
|------|-------------|
| Version | PostgreSQL **16** or later |
| Installation | Standalone server, managed service (Amazon RDS, Azure Database for PostgreSQL, Cloud SQL), or Docker container |
| User permissions | The Hub connection string user must have `CREATE`, `ALTER`, `SELECT`, `INSERT`, `UPDATE`, `DELETE` on the target database, and `CREATE TABLE` / `CREATE INDEX` for EF Core migrations |
| Encoding | `UTF8` (PostgreSQL default) |
| Collation | Any; `en-US-x-icu` or `C` recommended for consistency |

```bash
# Minimal PostgreSQL setup example
createuser --pwprompt edgeguard
createdb --owner=edgeguard edgeguard_hub
```

### Edge Node — SQLite

SQLite requires no external installation or configuration. The database file is created automatically at `./persistence/edge-node.db` on first startup. Ensure the process user has read/write access to the persistence directory.

---

## Network Ports

The following ports must be open between the indicated hosts. Adjust firewall rules before installation.

### Hub Server

| Port | Protocol | Direction | Purpose | Required? |
|------|----------|-----------|---------|-----------|
| **5000** | TCP | Inbound | Hub API HTTP (development / internal) | **Required** |
| **5001** | TCP | Inbound | Hub API HTTPS (production) | Recommended |
| **8001** | TCP | Inbound | HL7 MLLP listener (from HIS/RIS) | Required if HL7 enabled |
| **5432** | TCP | Outbound | PostgreSQL connection (to DB server) | **Required** |

### Edge Node

| Port | Protocol | Direction | Purpose | Required? |
|------|----------|-----------|---------|-----------|
| **11112** | TCP | Inbound | DICOM C-STORE / C-FIND / C-ECHO SCP (from modalities, PACS) | **Required** |
| **5000** | TCP | Outbound | HTTP to Hub API | **Required** |
| **5001** | TCP | Outbound | HTTPS to Hub API (production) | Recommended |
| **5120** | TCP | Inbound	| Node Api HTTP (development /iternal) | **Required**|
| PACS port | TCP | Outbound | DICOM C-STORE to PACS (commonly 104 or 11112) | Required for forwarding |

### Recommended Firewall Rules

```
# Hub: allow HL7 from HIS/RIS subnet only
iptables -A INPUT -p tcp --dport 8001 -s <HIS_SUBNET> -j ACCEPT
iptables -A INPUT -p tcp --dport 8001 -j DROP

# Hub: allow HTTPS from internal networks
iptables -A INPUT -p tcp --dport 5001 -s <INTERNAL_SUBNET> -j ACCEPT

# Edge Node: allow DICOM from modality subnet and PACS
iptables -A INPUT -p tcp --dport 11112 -s <MODALITY_SUBNET> -j ACCEPT
iptables -A INPUT -p tcp --dport 5120 -s <INTERNAL_SUBNET> -j ACCEPT
iptables -A INPUT -p tcp --dport 11112 -s <PACS_IP> -j ACCEPT
iptables -A INPUT -p tcp --dport 11112 -j DROP
```

> **Note:** On Windows, use Windows Firewall (`netsh advfirewall`) or Windows Defender Firewall with Advanced Security to create equivalent inbound rules.

---

## Minimum Hardware

### Hub Server

| Resource | Minimum | Recommended (production) |
|----------|---------|--------------------------|
| CPU cores | 2 | 4+ |
| RAM | 4 GB | 8 GB |
| Disk | 20 GB (OS + app) | 100 GB+ (depends on image metadata volume) |
| Disk type | HDD | SSD (PostgreSQL WAL performance) |
| Network | 100 Mbps | 1 Gbps |

> **Note:** The Hub stores only DICOM metadata (patient, study, series, instance UIDs), not pixel data. Pixel data resides on Edge Node SQLite and/or PACS. Hub disk requirements are therefore driven by audit log and HL7 message volume, not image size.

### Edge Node

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU cores | 1 | 2 |
| RAM | 1 GB | 2 GB |
| Disk | 50 GB | 500 GB+ (pixel data stored locally until forwarded) |
| Disk type | HDD | SSD (DICOM write throughput) |
| Network | 100 Mbps | 1 Gbps (CT/MR series are large) |

> **Note:** Edge Node disk usage is transient; studies are deleted after successful PACS delivery according to the `DicomServer:InstanceRetentionDays` policy. Size the disk to hold the peak backlog for your site's daily study volume multiplied by the retention period.

---

## Supported Operating Systems

| OS | Architecture | Hub | Edge Node | Notes |
|----|-------------|-----|-----------|-------|
| Windows 10 / 11 | x64 | Yes | Yes | Windows Service deployment supported |
| Windows Server 2022 | x64 | Yes | Yes | Recommended for production on Windows |
| Ubuntu 22.04 LTS | x64 | Yes | Yes | Recommended Linux distribution |
| Ubuntu 24.04 LTS | x64 | Yes | Yes | |
| Debian 12 (Bookworm) | x64 | Yes | Yes | |
| macOS 14+ (Sonoma) | arm64 / x64 | Dev only | Dev only | Not supported for production |
| Docker (Linux container) | x64 | Yes | Yes | See section 07 (Operations) |

---

## Optional Dependencies

| Dependency | Purpose | Configuration key |
|------------|---------|------------------|
| Seq | Centralized structured log viewer | `Seq:ServerUrl` |
| OpenTelemetry Collector | Distributed tracing and metrics export | `OpenTelemetry:Enabled`, `OpenTelemetry:Endpoint` |
| WhatsApp messaging gateway | Study-received and alert notifications | `Notifications:WhatsApp:GatewayUrl` |
| Reverse proxy (nginx / IIS / Caddy) | TLS termination, HTTP→HTTPS redirect | External configuration |


---

## Pre-Installation Checklist

Before running the quickstart guides, confirm:

- [ ] .NET 10 SDK installed and `dotnet --version` returns `10.x.x`
- [ ] Node.js 22+ installed and `node --version` returns `v22.x.x`
- [ ] Angular CLI 19 installed globally
- [ ] EF Core CLI installed globally
- [ ] PostgreSQL 16+ is running and accessible from the Hub server
- [ ] A PostgreSQL database and user have been created for EdgeGuard
- [ ] `HUB_DB_CONNECTION_STRING` environment variable is set (or ready to be set)
- [ ] Port 5000 (and optionally 5001) is open on the Hub server
- [ ] Port 8001 is open for HL7 traffic (if HL7 is enabled)
- [ ] Port 11112 is open on each Edge Node for DICOM traffic
- [ ] The `persistence/` directory on Edge Nodes is writable by the service account
