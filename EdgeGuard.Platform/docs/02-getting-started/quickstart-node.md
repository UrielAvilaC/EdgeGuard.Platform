# Quickstart — Edge Node

This guide covers installing and registering a single Edge Node at an imaging site as a **Windows Service**. Repeat the process for every site that has modalities sending DICOM studies.

**Prerequisites:** A running Hub instance (see [Quickstart — Hub](quickstart-hub.md)) and a Windows host that meets the [Edge Node hardware requirements](prerequisites.md).

---

## Before You Begin

On the Edge Node host (Windows), verify the .NET 10 Runtime is installed:

```powershell
dotnet --list-runtimes
# Expected: Microsoft.NETCore.App 10.0.x  [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
```

> If missing, install the **.NET 10 Runtime** (not the SDK, not the ASP.NET Core Hosting Bundle) from <https://dotnet.microsoft.com/download/dotnet/10.0>. The Node hosts Kestrel directly — no IIS is needed.

Confirm network connectivity from the Edge Node to the Hub:

```powershell
Invoke-RestMethod https://hub.your-org.local/health
# Expected: status = Healthy
```

Confirm DICOM port `11112` is free:

```powershell
Test-NetConnection -ComputerName localhost -Port 11112
# TcpTestSucceeded = False  (port not yet in use — good)
```

---

## Step 1 — Generate a Bootstrap Token in the Hub UI

1. Log in to the Hub at `https://hub.your-org.local`.
2. Navigate to **Nodes → Register New Node**.
3. Fill in:
   - **Display Name** — `Radiology Wing A`
   - **Node ID** — `node-rad-a` (must be unique, lowercase, hyphenated)
4. Click **Generate Bootstrap Token** and **copy the one-time token**.

> The bootstrap token can be redeemed only once. Keep it in a password manager until the Node is registered.

---

## Step 2 — Publish and Copy the Node Binaries

On a build machine with the .NET SDK:

```powershell
dotnet publish src\edge\Dicom.Edge.Node `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output C:\publish\EdgeGuard.Node
```

Transfer to the Node host (replace target as appropriate):

```powershell
robocopy C:\publish\EdgeGuard.Node \\node-host\C$\EdgeGuard\Node /MIR
```

Recommended target directory: `C:\EdgeGuard\Node\`.

---

## Step 3 — Configure `appsettings.Production.json`

On the Node host, create `C:\EdgeGuard\Node\appsettings.Production.json` (site-specific overrides — NEVER commit):

```jsonc
{
  "Node": {
    "NodeId":         "node-rad-a",
    "DisplayName":    "Radiology Wing A",
    "HubBaseUrl":     "https://hub.your-org.local",
    "BootstrapToken": "<paste the token from Step 1>"
  },
  "DicomServer": {
    "AeTitle":                 "EDGEGUARD_WINGA",
    "Port":                    11112,
    "MaxClients":              10,
    "ValidateCalledAe":        true,
    "AllowedCallingAeTitles":  [ "CT_GE_64", "MR_SIEMENS_3T" ],
    "ValidateCallingAe":       true,
    "MwlEnabled":              true,
    "CEchoEnabled":            true,
    "QrEnabled":               true
  },
  "Diagnostics": {
    "InstanceId": "NODE-RAD-A",
    "Redaction":  { "Enabled": true, "Mode": "Strict" }
  },
  "NodeAuth": { "Enforce": false }
}
```

### Key settings explained

| Key | Example | Description |
|-----|---------|-------------|
| `Node.NodeId` | `node-rad-a` | Unique across all nodes. Stable for the lifetime of the install. |
| `Node.HubBaseUrl` | `https://hub.your-org.local` | Full Hub URL. Must be HTTPS in production. |
| `Node.BootstrapToken` | `eyJ...` | One-time token issued in Step 1. Removed automatically after first successful registration. |
| `DicomServer.AeTitle` | `EDGEGUARD_WINGA` | Called AE Title for incoming DICOM associations. ≤16 chars, no spaces. |
| `DicomServer.Port` | `11112` | DICOM SCP TCP port. Must match what modalities are configured to send to. |
| `DicomServer.AllowedCallingAeTitles` | `[ "CT_GE_64", ... ]` | Whitelist of AE Titles allowed to connect (when `ValidateCallingAe=true`). |
| `Diagnostics.Redaction.Mode` | `Strict` | PHI redaction in logs. Always `Strict` on Nodes. |
| `NodeAuth.Enforce` | `false` | **P0-1** — set to `true` once the Hub side rolls out HMAC signing. |

---

## Step 4 — Open Firewall Ports

Allow modalities to reach the DICOM SCP and (optionally) admins to reach the health endpoint:

```powershell
# DICOM SCP — restrict to modality subnet
New-NetFirewallRule -DisplayName "EdgeGuard Node - DICOM SCP" `
  -Direction Inbound -Protocol TCP -LocalPort 11112 `
  -RemoteAddress 10.10.20.0/24 -Action Allow -Profile Domain,Private

# Admin/health endpoint — restrict to admin subnet
New-NetFirewallRule -DisplayName "EdgeGuard Node - Admin API" `
  -Direction Inbound -Protocol TCP -LocalPort 5001 `
  -RemoteAddress 10.10.99.0/24 -Action Allow -Profile Domain
```

---

## Step 5 — Pre-Create Data Folders and Grant Permissions

```powershell
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\logs        | Out-Null
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\persistence | Out-Null
New-Item -ItemType Directory -Force -Path C:\EdgeGuard\Node\data        | Out-Null

# Grant the service account Modify on writable folders (default account: NetworkService)
icacls C:\EdgeGuard\Node\logs        /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
icacls C:\EdgeGuard\Node\persistence /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
icacls C:\EdgeGuard\Node\data        /grant "NT AUTHORITY\NetworkService:(OI)(CI)M" /T
```

---

## Step 6 — Install as a Windows Service

```powershell
sc.exe create EdgeGuardNode `
  binPath= "C:\EdgeGuard\Node\Dicom.Edge.Node.exe" `
  DisplayName= "EdgeGuard Edge Node" `
  start= auto `
  obj= "NT AUTHORITY\NetworkService"

sc.exe description EdgeGuardNode "EdgeGuard Platform Edge Node - DICOM SCP, MWL, PACS routing"

# Recovery — restart after 10s / 30s / 60s on consecutive failures
sc.exe failure EdgeGuardNode reset= 86400 actions= restart/10000/restart/30000/restart/60000
sc.exe failureflag EdgeGuardNode 1

Start-Service EdgeGuardNode
```

> **Service account:** `NetworkService` is sufficient for outbound HTTP to the Hub and local file/SQLite I/O. If the DICOM SCU must authenticate against AD resources or network shares, use a dedicated AD service account: `obj= "DOMAIN\edgeguard_node_svc" password= "<pwd>"`.

---

## Step 7 — Verify Registration and Health

Tail the rolling log:

```powershell
Get-Content "C:\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log" -Wait -Tail 50
```

Expected first-run lines:

```
[INF] Starting EdgeGuard Edge Node 1.0.0 (NodeId=node-rad-a)
[INF] DICOM server listening on port 11112 — TLS=False C-STORE=enabled C-ECHO=True MWL=True QR=True
[INF] Node registering with Hub https://hub.your-org.local using bootstrap token
[INF] Node registration succeeded — ApiKey received and stored
[INF] Configuration sync received: 3 PACS destinations, 5 routing rules
[INF] Node is ready
```

Check the health endpoint:

```powershell
Invoke-RestMethod http://localhost:5001/health
# status: Healthy
# components: { DICOM: Healthy, SQLite: Healthy, HubConnection: Healthy }
```

Verify in the Hub UI: **Nodes → All Nodes** — the node now appears with status **Connected**.

> **Troubleshooting — `401 Unauthorized` during registration:** the bootstrap token has expired or was already consumed. Generate a new token in the Hub UI and update `appsettings.Production.json`.
>
> **Troubleshooting — `Port 11112 already in use`:** another DICOM SCP is running. Stop it (`Get-NetTCPConnection -LocalPort 11112` then identify the process) or change `DicomServer:Port`.
>
> **Troubleshooting — Service won't start:** check the **Windows Application Event Log** for `EdgeGuardNode` events. Common causes: missing .NET 10 Runtime, insufficient permissions on `logs\`, or invalid JSON in `appsettings.Production.json`.

---

## Step 8 — Verify DICOM Connectivity with C-ECHO

From your PACS or a DICOM utility (DCMTK `echoscu`, dcm4che `storescu`, fo-dicom-test):

```powershell
# Using DCMTK echoscu on the modality
echoscu.exe -aet CT_GE_64 -aec EDGEGUARD_WINGA <node-host> 11112

# Expected:
# I: Requesting Association
# I: Association Accepted (Max Send PDV: 16366)
# I: Sending Echo Request (MsgID 1)
# I: Received Echo Response (Success)
```

A successful C-ECHO confirms:

- The DICOM port is reachable from the modality subnet.
- The Node's AE Title is correctly configured.
- The calling AE is whitelisted (when `ValidateCallingAe=true`).
- DICOM association negotiation succeeds.

---

## What Gets Auto-Synced from the Hub

Once registered, the Hub pushes these whenever they change:

| Configuration | Sync trigger |
|---------------|--------------|
| PACS server list (AE Title, host, port, TLS, anonymize) | Hub PACS settings change |
| DICOM routing rules | Routing rule create / update / delete |
| HL7 worklist messages | HIS/RIS sends ORM to Hub |
| Node display name | Hub UI edit |

The Node also polls `GET /api/nodes/{id}/configuration` every 60s as a safety net if a push was missed.

---

## Manage the Service

```powershell
Get-Service EdgeGuardNode             # Status
Stop-Service EdgeGuardNode            # Stop
Start-Service EdgeGuardNode           # Start
Restart-Service EdgeGuardNode         # Restart

# Live logs
Get-Content "C:\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log" -Wait -Tail 50

# Uninstall
Stop-Service EdgeGuardNode
sc.exe delete EdgeGuardNode
```

---

## Optional — Linux systemd

For Linux hosts (containerized labs, CI, non-Windows sites), the Node also runs as a systemd service. See [deployment-node.md → Optional — Linux systemd](../07-operations/deployment-node.md#optional--linux-systemd).

---

## Summary

| Step | Action |
|------|--------|
| 1 | Generate bootstrap token in Hub UI |
| 2 | `dotnet publish` and copy binaries to `C:\EdgeGuard\Node` |
| 3 | Create `appsettings.Production.json` with `BootstrapToken`, `NodeId`, `AeTitle` |
| 4 | Open firewall ports 11112 (modality subnet) and 5001 (admin subnet) |
| 5 | Pre-create `logs\`, `persistence\`, `data\` and grant Modify to service account |
| 6 | `sc.exe create EdgeGuardNode … ; Start-Service EdgeGuardNode` |
| 7 | Verify node shows **Connected** in Hub UI and `/health` returns Healthy |
| 8 | Run C-ECHO from a modality / PACS to confirm DICOM connectivity |
