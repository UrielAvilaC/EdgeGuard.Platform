# Edge Node Deployment Guide

This guide covers deploying an EdgeGuard Edge Node as a Windows Service or Linux systemd service.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Data Directory Structure](#data-directory-structure)
3. [Configuration File](#configuration-file)
4. [Windows Service Installation](#windows-service-installation)
5. [Linux systemd Installation](#linux-systemd-installation)
6. [Hub Registration](#hub-registration)
7. [Firewall Configuration](#firewall-configuration)
8. [Update Process](#update-process)

---

## Prerequisites

| Component | Minimum | Notes |
|-----------|---------|-------|
| .NET Runtime | 10.0 | Worker Service host |
| RAM | 1 GB | 2 GB recommended for busy DICOM AEs |
| Disk | 10 GB | For temp DICOM files, SQLite, logs |
| Network | LAN access to Hub | Also needs DICOM port reachable from modalities |
| OS | Windows 10/Server 2019+ or Linux (systemd) | |

---

## Data Directory Structure

The node uses a predictable directory layout relative to the installation path:

```
/opt/edgeguard/node/          (or C:\EdgeGuard\Node\ on Windows)
  ├── Dicom.Edge.Node.dll     # Application binary
  ├── appsettings.json        # Base configuration
  ├── appsettings.Production.json  # Environment overrides
  ├── persistence/
  │   └── edge-node.db        # SQLite database (node state, routing rules)
  ├── logs/
  │   ├── node-20250101.log   # Daily rolling log files
  │   └── node-20250102.log
  └── data/
      └── (temp DICOM files)  # Transient; auto-cleaned after forwarding
```

> **Note:** The `data/` directory holds DICOM files in transit. Files are removed after successful forwarding to the PACS. This directory can be safely cleared if the node is stopped, though in-flight studies will need to be re-sent from the modality.

---

## Configuration File

**appsettings.json** (base — do not put secrets here):

```json
{
  "Node": {
    "NodeId": "node-site-a",
    "DisplayName": "Site A Edge Node",
    "HubBaseUrl": "https://your-hub-domain.example.com",
    "RegistrationKey": "YOUR_NODE_REGISTRATION_KEY"
  },
  "Dicom": {
    "AeTitle": "EDGEGUARD_NODE",
    "ListenPort": 11112,
    "MaxConcurrentConnections": 10,
    "MaxPduSize": 131072
  },
  "Diagnostics": {
    "PHIRedaction": "Strict",
    "Seq": {
      "Enabled": false,
      "Url": "",
      "ApiKey": ""
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

**appsettings.Production.json** (environment-specific, kept on the server):

```json
{
  "Node": {
    "HubBaseUrl": "https://hub.internal.example.com",
    "RegistrationKey": "prod-registration-key-from-hub-ui"
  },
  "Dicom": {
    "AeTitle": "EDGEGUARD_SITE_A"
  }
}
```

### Key Configuration Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `Node.NodeId` | Unique identifier for this node | `node-radiology-a` |
| `Node.HubBaseUrl` | URL of the Hub API | `https://hub.example.com` |
| `Node.RegistrationKey` | One-time key from Hub UI (see [Hub Registration](#hub-registration)) | `ey...` |
| `Dicom.AeTitle` | DICOM Application Entity Title for this node | `EDGEGUARD_NODE` |
| `Dicom.ListenPort` | DICOM C-STORE listen port | `11112` |
| `Dicom.MaxConcurrentConnections` | Max simultaneous DICOM associations | `10` |

---

## Windows Service Installation

### 1. Publish the application

```powershell
dotnet publish src/backend/Dicom.Edge.Node `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\EdgeGuard\Node
```

### 2. Create and start the service

```cmd
sc create EdgeGuardNode ^
  binPath= "C:\Program Files\dotnet\dotnet.exe C:\EdgeGuard\Node\Dicom.Edge.Node.dll" ^
  DisplayName= "EdgeGuard Edge Node" ^
  start= auto ^
  obj= "NT AUTHORITY\NetworkService"

sc description EdgeGuardNode "EdgeGuard Platform Edge Node - DICOM routing and HL7 integration"

sc start EdgeGuardNode
```

### 3. Configure service recovery

```cmd
sc failure EdgeGuardNode reset= 86400 actions= restart/10000/restart/30000/restart/60000
```

This restarts the service after 10 seconds on first failure, 30 seconds on second, 60 seconds on subsequent failures.

### 4. Verify the service

```powershell
sc query EdgeGuardNode
# Expected: STATE: 4 RUNNING

# Check health
Invoke-RestMethod http://localhost:5001/health
```

### 5. Configure log viewing

```powershell
# View recent logs
Get-Content C:\EdgeGuard\Node\logs\node-*.log -Tail 50
```

### Manage the service

```cmd
sc stop EdgeGuardNode    # Stop
sc start EdgeGuardNode   # Start
sc delete EdgeGuardNode  # Uninstall
```

---

## Linux systemd Installation

### 1. Publish the application

```bash
dotnet publish src/backend/Dicom.Edge.Node \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /opt/edgeguard/node

# Create dedicated user
sudo useradd --system --no-create-home --shell /sbin/nologin edgeguard-node

# Set ownership
sudo chown -R edgeguard-node:edgeguard-node /opt/edgeguard/node
```

### 2. Create systemd unit file

Create `/etc/systemd/system/edgeguard-node.service`:

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

# Environment
Environment=DOTNET_ENVIRONMENT=Production

# Restart policy
Restart=on-failure
RestartSec=10
KillMode=mixed
KillSignal=SIGTERM
TimeoutStopSec=30

# Security hardening
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/opt/edgeguard/node/logs /opt/edgeguard/node/data /opt/edgeguard/node/persistence

[Install]
WantedBy=multi-user.target
```

### 3. Enable and start

```bash
sudo systemctl daemon-reload
sudo systemctl enable edgeguard-node
sudo systemctl start edgeguard-node
sudo systemctl status edgeguard-node
```

### 4. View logs

```bash
# Via journald
journalctl -u edgeguard-node -f

# Via rolling log files
tail -f /opt/edgeguard/node/logs/node-$(date +%Y%m%d).log
```

---

## Hub Registration

Each node must register with the Hub before it can receive configuration and routing rules.

### Step 1: Generate a registration key in the Hub UI

1. Log in to the Hub at `https://your-hub-domain.example.com`.
2. Navigate to **Settings → Nodes → Register New Node**.
3. Fill in:
   - **Display Name**: Human-readable name (e.g., `Radiology Wing A`)
   - **Node ID**: Unique slug (e.g., `node-rad-a`)
4. Click **Generate Key**. Copy the one-time registration key.

### Step 2: Configure the node

Paste the key into `appsettings.Production.json` under `Node.RegistrationKey`.

### Step 3: Start the node

On first startup with a valid registration key, the node:
1. Contacts the Hub registration endpoint.
2. Receives its node certificate and configuration.
3. The key is consumed and the node appears as **Connected** in the Hub UI.

### What gets auto-synced from the Hub

Once registered, the following are pushed automatically whenever changed:

| Configuration | Sync trigger |
|---------------|-------------|
| PACS server list (AE Title, host, port) | Hub PACS settings change |
| DICOM routing rules | Routing rule create/update/delete |
| HL7 message type filters | Hub settings update |
| Node display name | Hub UI edit |

The node polls the Hub every 60 seconds for configuration changes and applies them without restart.

---

## Firewall Configuration

### Inbound rules (on the node host)

| Port | Protocol | Source | Purpose |
|------|----------|--------|---------|
| 11112 | TCP | Modality network segment | DICOM C-STORE (default, configurable) |
| 5001 | TCP | Internal only | Health/diagnostics endpoints |

### Outbound rules (from the node host)

| Destination | Port | Protocol | Purpose |
|-------------|------|----------|---------|
| Hub host | 443 | TCP | Hub API (registration, config sync) |
| PACS host | 104 or 11112 | TCP | DICOM C-STORE to PACS |

### Windows Firewall — open DICOM port

```powershell
New-NetFirewallRule `
  -DisplayName "EdgeGuard Node DICOM" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 11112 `
  -Action Allow `
  -Profile Domain,Private
```

### Linux iptables / firewalld

```bash
# firewalld
sudo firewall-cmd --permanent --add-port=11112/tcp
sudo firewall-cmd --reload

# or iptables
sudo iptables -A INPUT -p tcp --dport 11112 -j ACCEPT
```

---

## Update Process

Updating a node involves replacing the binary and restarting the service. Routing rules and PACS configuration are stored in the SQLite database and the Hub, so they are preserved across updates.

### Windows

```powershell
# 1. Stop the service
sc stop EdgeGuardNode

# 2. Publish new version over existing directory
dotnet publish src/backend/Dicom.Edge.Node `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\EdgeGuard\Node

# 3. Start the service
sc start EdgeGuardNode

# 4. Verify health
Invoke-RestMethod http://localhost:5001/health
```

### Linux

```bash
# 1. Stop the service
sudo systemctl stop edgeguard-node

# 2. Publish new version
dotnet publish src/backend/Dicom.Edge.Node \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained false \
  --output /opt/edgeguard/node

# Fix ownership (publish may create files as current user)
sudo chown -R edgeguard-node:edgeguard-node /opt/edgeguard/node

# 3. Start the service
sudo systemctl start edgeguard-node

# 4. Verify
sudo systemctl status edgeguard-node
curl http://localhost:5001/health
```

> **Tip:** The node's SQLite database (`persistence/edge-node.db`) is never overwritten by a publish. Always back up the database before major version upgrades. See [backup-recovery.md](./backup-recovery.md).
