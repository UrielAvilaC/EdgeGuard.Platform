# Edge Node Deployment Guide

EdgeGuard Edge Node is designed to run as a **Windows Service** on the imaging site host. This guide covers the production Windows Service deployment plus the optional Linux systemd alternative.

> **Target environment:** Windows Server 2019 / 2022 (or Windows 10 / 11 Pro for small sites) with .NET 10 Runtime + SQLite (bundled).

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Data Directory Structure](#data-directory-structure)
3. [Configuration File](#configuration-file)
4. [Windows Service Installation (Production)](#windows-service-installation-production)
5. [Hub Registration](#hub-registration)
6. [Firewall Configuration](#firewall-configuration)
7. [Update Process](#update-process)
8. [Verifying the Installation](#verifying-the-installation)
9. [Optional — Linux systemd](#optional--linux-systemd)

---

## Prerequisites

| Component | Minimum | Notes |
|-----------|---------|-------|
| Windows Server | 2019 | Or Windows 10 / 11 Pro for small sites |
| .NET Runtime | 10.0 | Install via `dotnet-runtime-10.0.x-win-x64.exe` (no IIS bundle needed — Node hosts Kestrel directly) |
| RAM | 2 GB | 4 GB recommended for busy DICOM AEs |
| Disk | 50 GB | Temp DICOM files + SQLite + rolling logs |
| Network | LAN access to Hub | Plus reachability from modalities on DICOM port |
| Administrator rights | Required | For `sc.exe` service install and firewall rules |

### Verify .NET Runtime

```powershell
dotnet --list-runtimes
# Expected:
# Microsoft.NETCore.App   10.0.0  [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
```

If missing, download the **.NET Runtime** (not the SDK, not the ASP.NET Core Hosting Bundle) from <https://dotnet.microsoft.com/download/dotnet/10.0>.

---

## Data Directory Structure

The Node uses a predictable directory layout under its installation path. **Recommended root:** `C:\EdgeGuard\Node\`.

```
C:\EdgeGuard\Node\
  ├── Dicom.Edge.Node.exe          # Service host
  ├── Dicom.Edge.Node.dll          # Application
  ├── appsettings.json             # Base configuration (committed-in defaults)
  ├── appsettings.Production.json  # Site-specific overrides (NOT committed)
  ├── persistence\
  │   └── edge-node.db             # SQLite DB (routing rules, queue, settings)
  ├── logs\
  │   ├── node-20260517.log        # Daily rolling log files
  │   └── node-20260518.log
  └── data\
      └── <StudyInstanceUid>\      # Temp DICOM files in transit (auto-cleaned)
          └── <SeriesInstanceUid>\
              └── <SopInstanceUid>.dcm
```

> **Cleanup behaviour:** files under `data\` are removed after successful forwarding to the PACS. This directory can be cleared while the service is stopped — in-flight studies will need to be re-sent from the modality.

---

## Configuration File

`appsettings.json` (base — defaults, no secrets):

```jsonc
{
  "Node": {
    "NodeId": "node-site-a",
    "DisplayName": "Site A Edge Node",
    "HubBaseUrl": "https://hub.your-org.local"
  },
  "DicomServer": {
    "AeTitle": "EDGEGUARD_NODE",
    "Port": 11112,
    "MaxClients": 10,
    "ValidateCalledAe": true,
    "MwlEnabled": true,
    "CEchoEnabled": true,
    "QrEnabled": true,

    // P0-3 — DICOM TLS (optional, opt-in)
    "Tls": {
      "Enabled": false,
      "CertificatePath": null,
      "CertificatePassword": null,
      "RequireClientCertificate": false
    }
  },
  "ConnectionStrings": {
    "NodeDatabase": "Data Source=./persistence/edge-node.db"
  },
  "Diagnostics": {
    "InstanceId": "NODE-001",
    "Application": "EdgeGuardNode",
    "Component": "Node",
    "Redaction": { "Enabled": true, "Mode": "Strict" }
  },
  "NodeAuth": {
    "Enforce": false   // P0-1 — flip to true once Hub deploy with signing is live
  }
}
```

`appsettings.Production.json` (site-specific, lives on the server — never commit):

```jsonc
{
  "Node": {
    "NodeId":     "node-radiology-wing-a",
    "DisplayName": "Radiology Wing A",
    "HubBaseUrl":  "https://hub.your-org.local"
  },
  "DicomServer": {
    "AeTitle": "EDGEGUARD_WINGA",
    "AeTitleAliases": [ "OLDEDGE", "RAD_AE" ],
    "AllowedCallingAeTitles": [ "CT_GE_64", "MR_SIEMENS_3T" ],
    "ValidateCallingAe": true
  }
}
```

### Key configuration settings

| Setting | Description | Example |
|---------|-------------|---------|
| `Node.NodeId` | Unique identifier for this node | `node-radiology-a` |
| `Node.HubBaseUrl` | Hub URL | `https://hub.your-org.local` |
| `DicomServer.AeTitle` | DICOM AE Title for this node's SCP | `EDGEGUARD_NODE` |
| `DicomServer.Port` | DICOM C-STORE listen port | `11112` |
| `DicomServer.MaxClients` | Max simultaneous DICOM associations | `10` |
| `DicomServer.Tls.Enabled` | Enable TLS on the DICOM SCP listener (P0-3) | `true` once certificate is provisioned |
| `NodeAuth.Enforce` | Reject unsigned Hub→Node requests (P0-1) | `true` after coordinated Hub deploy |

---

## Windows Service Installation (Production)

### 1. Publish the application

From a build machine with the .NET SDK:

```powershell
dotnet publish src\edge\Dicom.Edge.Node `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\EdgeGuard\Node
```

> The publish output contains `Dicom.Edge.Node.exe` (the Windows host shim) plus `Dicom.Edge.Node.dll` (the actual app). Both must exist for `sc.exe` to start the service.

### 2. Pre-create data folders with correct permissions

```powershell
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\logs        | Out-Null
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\persistence | Out-Null
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\data        | Out-Null

# Grant the service account Modify rights on the writable folders.
# Replace NT AUTHORITY\NetworkService with your dedicated service account if applicable.
icacls C:\EdgeGuard\Node\logs        /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
icacls C:\EdgeGuard\Node\persistence /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
icacls C:\EdgeGuard\Node\data        /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
```

### 3. Create and start the service

```powershell
# Create the service
sc.exe create EdgeGuardNode `
  binPath= "C:\EdgeGuard\Node\Dicom.Edge.Node.exe" `
  DisplayName= "EdgeGuard Edge Node" `
  start= auto `
  obj= "NT AUTHORITY\NetworkService"

sc.exe description EdgeGuardNode "EdgeGuard Platform Edge Node - DICOM C-STORE SCP, MWL, and PACS routing"
```

> **Service account:** `NetworkService` is sufficient for outbound calls and local file/SQLite access. For environments where DICOM SCU must authenticate against domain resources or write to network shares, use a dedicated AD service account: `obj= "DOMAIN\edgeguard_node_svc" password= "<pwd>"`.

### 4. Configure recovery policy

```powershell
# Restart after 10s on 1st failure, 30s on 2nd, 60s on subsequent. Reset counter daily.
sc.exe failure EdgeGuardNode reset= 86400 actions= restart/10000/restart/30000/restart/60000

# Restart only on unexpected exit (not on graceful Stop)
sc.exe failureflag EdgeGuardNode 1
```

### 5. Start the service

```powershell
Start-Service EdgeGuardNode

# Verify
Get-Service EdgeGuardNode
# Status   Name           DisplayName
# ------   ----           -----------
# Running  EdgeGuardNode  EdgeGuard Edge Node
```

### 6. Tail the log

```powershell
Get-Content "C:\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log" -Wait -Tail 50
```

Expected first-run lines:

```
[INF] Starting EdgeGuard Edge Node 1.0.0 (NodeId=node-site-a)
[INF] DICOM server listening on port 11112 — TLS=False C-STORE=enabled C-ECHO=True MWL=True QR=True
[INF] Node registered with Hub https://hub.your-org.local — ApiKey received
[INF] Configuration sync received: 3 PACS destinations, 5 routing rules
```

### Manage the service

```powershell
Stop-Service    EdgeGuardNode
Start-Service   EdgeGuardNode
Restart-Service EdgeGuardNode

# Uninstall completely
Stop-Service EdgeGuardNode
sc.exe delete EdgeGuardNode
```

---

## Hub Registration

Each Node must register with the Hub before it can receive PACS destinations and routing rules.

### Step 1 — Generate a bootstrap token in the Hub UI

1. Log in to the Hub at `https://hub.your-org.local`.
2. Navigate to **Nodes → Register New Node**.
3. Fill in:
   - **Display Name** — human-readable name (e.g. `Radiology Wing A`)
   - **Node ID** — unique slug (e.g. `node-rad-a`)
4. Click **Generate Bootstrap Token** and copy the one-time token.

### Step 2 — Configure the Node

Edit `C:\EdgeGuard\Node\appsettings.Production.json`:

```jsonc
{
  "Node": {
    "NodeId":            "node-rad-a",
    "BootstrapToken":    "<paste token here>",
    "HubBaseUrl":        "https://hub.your-org.local"
  }
}
```

### Step 3 — Start (or restart) the service

```powershell
Restart-Service EdgeGuardNode
```

On first startup with a valid bootstrap token, the Node:

1. Calls `POST /api/edge/register` on the Hub with the bootstrap token.
2. Receives a permanent **ApiKey** that is persisted in `node_settings` (key `hub.api_key`).
3. Removes the consumed `BootstrapToken` from `appsettings.Production.json` automatically (or you can remove it manually).
4. Appears as **Connected** in the Hub UI.

### What gets auto-synced from the Hub

Once registered, these are pushed automatically whenever changed:

| Configuration | Sync trigger |
|---------------|--------------|
| PACS server list (AE Title, host, port, TLS, anonymize) | Hub PACS settings change |
| DICOM routing rules | Routing rule create / update / delete |
| HL7 worklist messages | HIS/RIS sends to Hub |
| Node display name | Hub UI edit |

The Node also polls `GET /api/nodes/{id}/configuration` every 60s as a safety net in case a push was lost while the Node was offline.

---

## Firewall Configuration

### Inbound rules (on the Node host)

| Port | Protocol | Source | Purpose |
|------|----------|--------|---------|
| 11112 | TCP | Modality network segment | DICOM C-STORE / C-FIND MWL / C-ECHO |
| 5001 | TCP | Internal admin subnet | Health / configuration endpoints |

### Outbound rules (from the Node host)

| Destination | Port | Protocol | Purpose |
|-------------|------|----------|---------|
| Hub | 443 | TCP | Hub API (registration, sync, telemetry) |
| PACS | 104 or 11112 | TCP | DICOM C-STORE SCU |

### Windows Firewall rules

```powershell
# Allow DICOM C-STORE/C-FIND inbound from modality subnet
New-NetFirewallRule `
  -DisplayName "EdgeGuard Node - DICOM SCP" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 11112 `
  -RemoteAddress 10.10.20.0/24 `   # Modality subnet
  -Action Allow `
  -Profile Domain,Private

# Allow health/admin endpoints from admin subnet only
New-NetFirewallRule `
  -DisplayName "EdgeGuard Node - Admin API" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5001 `
  -RemoteAddress 10.10.99.0/24 `   # Admin subnet
  -Action Allow `
  -Profile Domain
```

---

## Update Process

Updates replace the binaries and restart the service. Routing rules, registration, and PACS configuration live in SQLite (`persistence\edge-node.db`) and the Hub, so they survive the upgrade unchanged.

```powershell
# 1. Stop the service
Stop-Service EdgeGuardNode

# 2. Optional — back up the SQLite database
$stamp = Get-Date -Format yyyyMMdd-HHmmss
Copy-Item C:\EdgeGuard\Node\persistence\edge-node.db `
          C:\EdgeGuard\Backups\edge-node.$stamp.db

# 3. Publish the new version over the existing directory.
#    --no-self-contained ensures we only ship binaries (NOT the .NET runtime).
dotnet publish src\edge\Dicom.Edge.Node `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\EdgeGuard\Node

# 4. Start the service
Start-Service EdgeGuardNode

# 5. Verify health
Invoke-RestMethod http://localhost:5001/health
```

> **Tip:** Migrations on the SQLite DB run automatically at startup. If a migration fails, the service log will report it and the service will exit — restore from backup, fix the issue, and try again.

---

## Verifying the Installation

### Service status

```powershell
Get-Service EdgeGuardNode
# Status: Running

# Detailed
sc.exe queryex EdgeGuardNode
```

### Health endpoint

```powershell
Invoke-RestMethod http://localhost:5001/health
# Expected:
# status      : Healthy
# components  : { DICOM: Healthy, SQLite: Healthy, HubConnection: Healthy }
```

### DICOM C-ECHO verification (from another DICOM tool)

From any DICOM tool that supports C-ECHO (DCMTK, fo-dicom-test, etc.):

```bash
echoscu -aec EDGEGUARD_NODE -aet TEST <node-host> 11112
# Expected: I: Echo Response: Success [0000h]
```

### Hub registration

In the Hub UI: **Nodes → All Nodes** — the newly-registered node should appear with status **Connected** and the configured AE Title.

---

## Optional — Linux systemd

For non-Windows environments (containerized labs, Linux-only sites), the Node can also run as a systemd service.

```bash
# Publish for Linux
dotnet publish src/edge/Dicom.Edge.Node \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /opt/edgeguard/node

# Dedicated user
sudo useradd --system --no-create-home --shell /sbin/nologin edgeguard-node
sudo chown -R edgeguard-node:edgeguard-node /opt/edgeguard/node
```

`/etc/systemd/system/edgeguard-node.service`:

```ini
[Unit]
Description=EdgeGuard Platform Edge Node
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
User=edgeguard-node
Group=edgeguard-node
WorkingDirectory=/opt/edgeguard/node
ExecStart=/usr/bin/dotnet /opt/edgeguard/node/Dicom.Edge.Node.dll
Environment=DOTNET_ENVIRONMENT=Production
Restart=on-failure
RestartSec=10
KillMode=mixed
KillSignal=SIGTERM
TimeoutStopSec=30
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/opt/edgeguard/node/logs /opt/edgeguard/node/data /opt/edgeguard/node/persistence

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable edgeguard-node
sudo systemctl start  edgeguard-node
sudo systemctl status edgeguard-node
journalctl -u edgeguard-node -f
```

> **Not the primary deployment target.** Windows Service is canonical; Linux is kept for parity and is used by CI/dev environments.
