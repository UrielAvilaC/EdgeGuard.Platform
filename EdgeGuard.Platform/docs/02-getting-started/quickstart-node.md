# Quickstart — Edge Node

This guide covers installing and registering a single Edge Node at an imaging site. Repeat this process for every site that has modalities sending DICOM studies.

**Prerequisites:** A running Hub instance (see [Quickstart — Hub](quickstart-hub.md)) and a target machine that meets the [Edge Node hardware requirements](prerequisites.md).

---

## Before You Begin

On the Edge Node machine, verify the .NET 10 runtime is installed:

```bash
dotnet --version   # must return 10.x.x
```

Confirm network connectivity from the Edge Node machine to the Hub:

```bash
# Replace hub.internal with your Hub's hostname or IP
curl -f http://hub.internal:5000/api/health
# Expected: {"status":"Healthy"}
```

Confirm DICOM port availability:

```bash
# Linux
ss -tlnp | grep 11112   # should return nothing (port not yet in use)

# Windows
netstat -ano | findstr :11112
```

---

## Step 1 — Install on the Target Machine

### Option A — Copy Published Binaries (Recommended for production)

On a build machine with the .NET SDK:

```bash
dotnet publish src/edge/Dicom.Edge.Node \
  --configuration Release \
  --output ./publish/edge-node \
  --self-contained false
```

Transfer the `publish/edge-node/` directory to the target machine (e.g., via SCP, SMB share, or deployment package).

### Option B — Build and Run from Source

If the .NET SDK is installed on the target machine:

```bash
git clone https://github.com/your-org/EdgeGuard.Platform.git
cd EdgeGuard.Platform/EdgeGuard.Platform
```

---

## Step 2 — Configure `appsettings.json`

Navigate to the Edge Node directory and open `appsettings.json` (or create `appsettings.Production.json` for environment-specific overrides).

```json
{
  "ConnectionStrings": {
    "NodeDatabase": "./persistence/edge-node.db"
  },
  "Hub": {
    "BaseUrl": "http://hub.internal:5000",
    "NodeId": "EDGE-SITE-01"
  },
  "DicomServer": {
    "AeTitle": "EDGE_SITE01",
    "Port": 11112,
    "MaxConcurrentAssociations": 10
  },
  "DicomServer": {
    "InstanceRetentionDays": 7
  },
  "Diagnostics": {
    "InstanceId": "NODE-001",
    "Redaction": {
      "Mode": "Strict"
    }
  }
}
```

### Key Settings Explained

| Key | Example Value | Description |
|-----|--------------|-------------|
| `Hub:BaseUrl` | `http://hub.internal:5000` | Full URL of the Hub API. Use HTTPS in production. |
| `Hub:NodeId` | `EDGE-SITE-01` | Unique identifier for this node across the entire platform. Use a descriptive, stable name. |
| `DicomServer:AeTitle` | `EDGE_SITE01` | AE Title that modalities and PACS use to address this node. Max 16 characters, no spaces. |
| `DicomServer:Port` | `11112` | TCP port for the DICOM SCP. Must match the AE configuration on connected modalities. |
| `DicomServer:InstanceRetentionDays` | `7` | Days to retain delivered DICOM instances in SQLite before automatic deletion. |
| `Diagnostics:InstanceId` | `NODE-001` | Human-readable identifier included in every log entry from this node. |
| `Diagnostics:Redaction:Mode` | `Strict` | PHI redaction level. `Strict` (recommended for nodes) redacts all patient fields from logs. |

> **Note:** The `Hub:NodeId` value must be unique across all registered nodes. If you deploy multiple nodes, use site-specific identifiers such as `EDGE-RADIOLOGY-FLOOR2` or `EDGE-CT-SUITE-A`.

---

## Step 3 — Start the Edge Node

```bash
# From the published output directory
dotnet Dicom.Edge.Node.dll

# Or from source
dotnet run --project src/edge/Dicom.Edge.Node
```

Successful startup output:

```
[INF] EdgeGuard Edge Node starting. NodeId=EDGE-SITE-01 AeTitle=EDGE_SITE01
[INF] DICOM SCP listening on 0.0.0.0:11112
[INF] Attempting registration with Hub at http://hub.internal:5000
[INF] Node registered successfully. NodeToken issued.
[INF] Edge Node is ready.
```

> **Troubleshooting:** If you see `Failed to register with Hub: 401 Unauthorized`, the Hub may require a node pre-registration secret. Check whether `Hub:RegistrationSecret` needs to be set (configured in Hub appsettings under `NodeRegistration:Secret`).

> **Troubleshooting:** If DICOM port 11112 fails to bind, another process (e.g., a pre-existing DICOM SCP) may be using the port. Stop the conflicting service or change `DicomServer:Port`.

---

## Step 4 — Register the Node in the Hub UI

1. Log into the Hub SPA at `http://hub.internal:5000`.
2. Navigate to **Nodes** in the left sidebar.
3. If auto-registration is enabled (default), the node already appears in the list with status **Healthy**.
4. If the node is not listed, click **Add Node** and fill in:
   - **Node ID:** `EDGE-SITE-01` (must match `Hub:NodeId` in the node's appsettings)
   - **AE Title:** `EDGE_SITE01`
   - **IP Address / Hostname:** The node's network address as reachable from the Hub
   - **API Port:** The management API port (default: 5002)
5. Click **Save**. The Hub performs a connectivity check and marks the node **Healthy** if successful.

---

## Step 5 — Hub Pushes PACS Destinations and Routing Rules

After registration, the Hub automatically pushes the node's initial configuration:

- **PACS destinations** matching the node's site or the "all nodes" default list.
- **Routing rules** applicable to this node.

You can verify the push succeeded from the **Nodes** detail page:

1. Click the node name in the list.
2. Check the **Last Config Push** timestamp and **Routing Rules** count.
3. The **Applied Config** section shows the PACS destinations and rules currently active on the node.

To manually trigger a config push (useful after adding a new routing rule):

- Click **Push Config** on the node detail page, or
- Make a Hub API call: `POST /api/nodes/{nodeId}/push-config`

---

## Step 6 — Verify Connectivity with a C-ECHO Test

From your PACS administration tool or a DICOM utility (e.g., `echoscu` from the DCMTK toolkit), send a C-ECHO to the Edge Node:

```bash
# Using DCMTK echoscu
echoscu -aet MY_PACS -aec EDGE_SITE01 <node-ip> 11112

# Expected output:
# I: Requesting Association
# I: Association Accepted (Max Send PDV: 16366)
# I: Sending Echo Request (MsgID 1)
# I: Received Echo Response (Success)
# I: Releasing Association
```

A successful C-ECHO confirms:
- The DICOM port is reachable from the PACS/modality network.
- The Edge Node's AE Title is correctly configured.
- DICOM association negotiation succeeds.

> **Troubleshooting:** If the C-ECHO fails with `Connection refused`, verify that the Edge Node process is running and the firewall allows inbound TCP on port 11112. If it fails with `Association Rejected`, check that the calling AE Title (`-aet MY_PACS`) is in the Edge Node's AE Title allow-list (configurable in `DicomServer:AllowedCallingAeTitles`).

---

## Long-Running Deployment

### Windows — Register as a Windows Service

```powershell
# Run as Administrator
$exePath = "C:\EdgeGuard\Dicom.Edge.Node.exe"
New-Service -Name "EdgeGuardNode" `
            -BinaryPathName $exePath `
            -DisplayName "EdgeGuard Edge Node" `
            -StartupType Automatic

Start-Service EdgeGuardNode
```

Set environment-specific configuration using the Windows Service environment or a `appsettings.Production.json` file in the same directory as the executable.

### Linux — Register as a systemd Service

Create `/etc/systemd/system/edgeguard-node.service`:

```ini
[Unit]
Description=EdgeGuard Edge Node
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/edgeguard/node
ExecStart=/usr/bin/dotnet /opt/edgeguard/node/Dicom.Edge.Node.dll
Restart=always
RestartSec=10
User=edgeguard
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable edgeguard-node
sudo systemctl start edgeguard-node
sudo systemctl status edgeguard-node
```

View logs:

```bash
journalctl -u edgeguard-node -f
```

> **Note:** Create a dedicated `edgeguard` system user without login shell for running the service: `sudo useradd --system --no-create-home --shell /usr/sbin/nologin edgeguard`. Grant ownership of the working directory and persistence folder to this user.

---

## Summary

| Step | Action |
|------|--------|
| 1 | Install .NET runtime; copy published binaries to target machine |
| 2 | Configure `appsettings.json` with HubUrl, NodeId, AeTitle, Port |
| 3 | `dotnet Dicom.Edge.Node.dll` — node auto-registers with Hub |
| 4 | Verify node appears in Hub UI → Nodes |
| 5 | Hub pushes PACS destinations and routing rules automatically |
| 6 | Run C-ECHO from PACS to verify DICOM connectivity |
| (prod) | Register as Windows Service or systemd unit for persistent operation |
