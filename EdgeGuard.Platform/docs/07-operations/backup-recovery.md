# Backup and Recovery

This runbook covers what to back up, how to back it up, and how to recover EdgeGuard Platform from backup.

---

## Table of Contents

1. [Backup Scope](#backup-scope)
2. [RPO and RTO Targets](#rpo-and-rto-targets)
3. [PostgreSQL Backup (Hub)](#postgresql-backup-hub)
4. [SQLite Backup (Edge Nodes)](#sqlite-backup-edge-nodes)
5. [Configuration and Certificates](#configuration-and-certificates)
6. [Transient Data (DICOM temp files)](#transient-data-dicom-temp-files)
7. [Recovery Procedure](#recovery-procedure)
8. [Backup Verification](#backup-verification)

---

## Backup Scope

| Component | Location (Windows — primary) | Backup required | Frequency |
|-----------|------------------------------|-----------------|-----------|
| Hub database (PostgreSQL) | Hub server | Yes — critical | Daily |
| Node database (SQLite) | `C:\EdgeGuard\Node\persistence\edge-node.db` | Yes — recommended | Daily |
| Hub appsettings | `C:\inetpub\EdgeGuard\Hub\appsettings.Production.json` (+ IIS App Pool env vars) | Yes | On change |
| Node appsettings | `C:\EdgeGuard\Node\appsettings.Production.json` | Yes | On change |
| SSL/TLS certificates | Hub server (Windows cert store or `C:\inetpub\EdgeGuard\Hub\certs\`) | Yes | On renewal |
| DICOM temp files | `C:\EdgeGuard\Node\data\` | No — transient | N/A |
| Log files | `C:\inetpub\EdgeGuard\Hub\logs\`, `C:\EdgeGuard\Node\logs\` | Optional | Offload to SIEM |

> **Linux equivalents (secondary, optional):** Hub config at `/etc/edgeguard/hub.env` + `/opt/edgeguard/hub/`, Node DB at `/opt/edgeguard/node/persistence/edge-node.db`, temp/logs under `./data/` and `./logs/`.

---

## RPO and RTO Targets

| Metric | Target | Achieved by |
|--------|--------|-------------|
| Recovery Point Objective (RPO) | 24 hours maximum data loss | Daily database backup |
| Recovery Time Objective (RTO) | < 1 hour to restore service | Documented recovery procedure |

For higher availability requirements (RPO < 1h), consider PostgreSQL streaming replication to a standby server.

---

## PostgreSQL Backup (Hub)

### Daily backup script (Windows — primary)

Create `C:\EdgeGuard\Scripts\Backup-HubDb.ps1`:

```powershell
$ErrorActionPreference = "Stop"

$BackupDir     = "C:\Backups\EdgeGuard\Hub"
$DbName        = "edgeguard_hub"
$DbUser        = "edgeguard"
$RetentionDays = 14

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

$Timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = Join-Path $BackupDir "${DbName}_${Timestamp}.dump"

Write-Host "[$(Get-Date -Format o)] Starting PostgreSQL backup: $BackupFile"

$env:PGPASSWORD = $env:POSTGRES_PASSWORD
& "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe" `
    --host=localhost --port=5432 --username=$DbUser `
    --format=custom --compress=9 --file=$BackupFile $DbName

$Size = (Get-Item $BackupFile).Length / 1MB
Write-Host "[$(Get-Date -Format o)] Backup complete. Size: $([math]::Round($Size,2)) MB"

# Remove backups older than retention period
Get-ChildItem $BackupDir -Filter "*.dump" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } |
    Remove-Item -Force
```

Register as a Windows Scheduled Task (daily at 02:00):

```powershell
$Action  = New-ScheduledTaskAction -Execute "powershell.exe" `
            -Argument "-NoProfile -File C:\EdgeGuard\Scripts\Backup-HubDb.ps1"
$Trigger = New-ScheduledTaskTrigger -Daily -At 02:00
Register-ScheduledTask -TaskName "EdgeGuard-Backup-HubDb" `
    -Action $Action -Trigger $Trigger -User "SYSTEM" -RunLevel Highest
```

### Manual on-demand backup (PowerShell)

```powershell
$env:PGPASSWORD = "your_password"
& "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe" `
    --host=localhost --port=5432 --username=edgeguard `
    --format=custom --compress=9 `
    --file="C:\Backups\EdgeGuard\Hub\manual_$(Get-Date -Format yyyyMMdd_HHmmss).dump" `
    edgeguard_hub
```

### If on Linux (alternative)

Create `/opt/edgeguard/scripts/backup-hub-db.sh`:

```bash
#!/bin/bash
set -euo pipefail

BACKUP_DIR="/var/backups/edgeguard/hub"
DB_NAME="edgeguard_hub"
DB_USER="edgeguard"
RETENTION_DAYS=14

mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.dump"

PGPASSWORD="$POSTGRES_PASSWORD" pg_dump \
  --host=localhost --port=5432 --username="$DB_USER" \
  --format=custom --compress=9 --file="$BACKUP_FILE" "$DB_NAME"

find "$BACKUP_DIR" -name "*.dump" -mtime +"$RETENTION_DAYS" -delete
```

Schedule via cron:

```bash
chmod +x /opt/edgeguard/scripts/backup-hub-db.sh
echo "0 2 * * * edgeguard POSTGRES_PASSWORD=your_password /opt/edgeguard/scripts/backup-hub-db.sh >> /var/log/edgeguard-backup.log 2>&1" \
  | sudo tee -a /etc/cron.d/edgeguard-backup
```

### Backup to remote storage (S3-compatible)

**PowerShell (Windows):**
```powershell
# After pg_dump completes, upload to S3
aws s3 cp $BackupFile "s3://your-backup-bucket/edgeguard/hub/" `
  --storage-class STANDARD_IA `
  --sse AES256
```

**Bash (Linux, alternative):**
```bash
aws s3 cp "$BACKUP_FILE" "s3://your-backup-bucket/edgeguard/hub/" \
  --storage-class STANDARD_IA \
  --sse AES256
```

---

## SQLite Backup (Edge Nodes)

Each node uses a SQLite database at `C:\EdgeGuard\Node\persistence\edge-node.db` (Windows — primary) or `/opt/edgeguard/node/persistence/edge-node.db` (Linux — alternative). SQLite supports online backup via the `.backup` command (WAL-aware, safe while the node is running) or a simple file copy when the Windows Service `EdgeGuardNode` is stopped.

### Option A: Online backup (PowerShell, Windows — primary)

```powershell
# WAL-safe; does NOT require stopping the EdgeGuardNode service
sqlite3 C:\EdgeGuard\Node\persistence\edge-node.db `
  ".backup 'C:\Backups\EdgeGuard\Node\edge-node-$(Get-Date -Format yyyyMMdd_HHmmss).db'"
```

### Option B: File copy with service stopped (PowerShell, Windows)

```powershell
Stop-Service EdgeGuardNode

Copy-Item "C:\EdgeGuard\Node\persistence\edge-node.db" `
          "C:\Backups\EdgeGuard\Node\edge-node-$(Get-Date -Format yyyyMMdd_HHmmss).db"

Start-Service EdgeGuardNode
```

### If on Linux (alternative)

```bash
# Online backup
sqlite3 /opt/edgeguard/node/persistence/edge-node.db \
  ".backup '/var/backups/edgeguard/node/edge-node-$(date +%Y%m%d_%H%M%S).db'"

# Or file copy with service stopped
sudo systemctl stop edgeguard-node
cp /opt/edgeguard/node/persistence/edge-node.db \
   /var/backups/edgeguard/node/edge-node-$(date +%Y%m%d_%H%M%S).db
sudo systemctl start edgeguard-node
```

---

## Configuration and Certificates

### Hub configuration files (PowerShell, Windows — primary)

```powershell
$Date = Get-Date -Format yyyyMMdd

# Backup appsettings + IIS App Pool config (env vars are stored in applicationHost.config)
Compress-Archive -Path `
    "C:\inetpub\EdgeGuard\Hub\appsettings.json", `
    "C:\inetpub\EdgeGuard\Hub\appsettings.Production.json", `
    "C:\Windows\System32\inetsrv\config\applicationHost.config" `
    -DestinationPath "C:\Backups\EdgeGuard\Hub\config-$Date.zip"

# Export TLS certificate from Windows cert store (LocalMachine\My)
$Cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -Like "*edgeguard*"
Export-PfxCertificate -Cert $Cert `
    -FilePath "C:\Backups\EdgeGuard\Hub\edgeguard-$Date.pfx" `
    -Password (ConvertTo-SecureString -String "your_pfx_password" -AsPlainText -Force)
```

> **Security:** Store certificate backups in a separate, access-controlled location. Never commit certificate files to version control.

### Node configuration files (PowerShell, Windows — primary)

```powershell
Compress-Archive -Path `
    "C:\EdgeGuard\Node\appsettings.json", `
    "C:\EdgeGuard\Node\appsettings.Production.json" `
    -DestinationPath "C:\Backups\EdgeGuard\Node\config-$(Get-Date -Format yyyyMMdd).zip"
```

### If on Linux (alternative)

```bash
tar -czf /var/backups/edgeguard/hub/config-$(date +%Y%m%d).tar.gz \
  /etc/edgeguard/hub.env \
  /opt/edgeguard/hub/appsettings.json \
  /opt/edgeguard/hub/appsettings.Production.json

tar -czf /var/backups/edgeguard/hub/certs-$(date +%Y%m%d).tar.gz \
  /etc/ssl/certs/edgeguard.crt \
  /etc/ssl/private/edgeguard.key

tar -czf /var/backups/edgeguard/node/config-$(date +%Y%m%d).tar.gz \
  /opt/edgeguard/node/appsettings.json \
  /opt/edgeguard/node/appsettings.Production.json
```

---

## Transient Data (DICOM temp files)

The `C:\EdgeGuard\Node\data\` directory (Windows) or `./data/` directory (Linux) on each node holds DICOM files currently in transit (received from modality, awaiting forwarding to PACS). This data is **transient and not included in backup scope**.

- Files are removed automatically after successful PACS forwarding.
- If a node fails with studies still in flight, those studies must be re-sent from the originating modality.
- This is acceptable within the defined RPO — modalities can be asked to re-send if needed.

For environments where re-send from modality is not practical, consider placing the data directory on network-attached storage (SMB/iSCSI on Windows, NFS on Linux) with its own redundancy.

---

## Recovery Procedure

### Full Hub recovery after server loss (Windows — primary)

**Step 1: Provision replacement Windows Server**

Install Windows Server, .NET 10 Hosting Bundle, IIS (with WebSocket support), PostgreSQL 16, and restore configuration files:

```powershell
Expand-Archive -Path "C:\Backups\EdgeGuard\Hub\config-20250101.zip" `
               -DestinationPath "C:\inetpub\EdgeGuard\Hub\" -Force

# Import the TLS certificate back into LocalMachine\My
Import-PfxCertificate -FilePath "C:\Backups\EdgeGuard\Hub\edgeguard-20250101.pfx" `
    -CertStoreLocation Cert:\LocalMachine\My `
    -Password (ConvertTo-SecureString -String "your_pfx_password" -AsPlainText -Force)
```

**Step 2: Restore the PostgreSQL database**

```powershell
$env:PGPASSWORD = "postgres_admin_password"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "CREATE USER edgeguard WITH PASSWORD 'your_password';"
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -U postgres -c "CREATE DATABASE edgeguard_hub OWNER edgeguard;"

$env:PGPASSWORD = "your_password"
& "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe" `
    --host=localhost --port=5432 --username=edgeguard `
    --dbname=edgeguard_hub --no-owner --role=edgeguard `
    "C:\Backups\EdgeGuard\Hub\edgeguard_hub_20250101_020000.dump"
```

**Step 3: Recreate the IIS App Pool and Site, deploy the Hub application**

```powershell
Import-Module WebAdministration

# Create app pool (No Managed Code = required for ASP.NET Core in-process hosting)
New-WebAppPool -Name "EdgeGuardHub"
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name "startMode" -Value "AlwaysRunning"
Set-ItemProperty IIS:\AppPools\EdgeGuardHub -Name "processModel.idleTimeout" -Value "00:00:00"

# Publish into IIS root
dotnet publish src/backend/Dicom.Edge.Hub.Api `
  --configuration Release `
  --output C:\inetpub\EdgeGuard\Hub

# Create site bound to App Pool
New-Website -Name "EdgeGuardHub" `
    -PhysicalPath "C:\inetpub\EdgeGuard\Hub" `
    -ApplicationPool "EdgeGuardHub" `
    -Port 443 -Ssl

Start-WebAppPool -Name "EdgeGuardHub"
```

**Step 4: Verify health**

```powershell
Invoke-RestMethod https://hub.your-org.local/health
# Expected: status = Healthy
```

**Step 5: Verify nodes reconnect**

In the Hub UI, check that all nodes show **Connected** status within 2–3 minutes of Hub restart.

### Node recovery after hardware failure (Windows — primary)

**Step 1:** Install .NET 10 Runtime on the replacement Windows host.

**Step 2: Restore configuration:**

```powershell
Expand-Archive -Path "C:\Backups\EdgeGuard\Node\config-20250101.zip" `
               -DestinationPath "C:\EdgeGuard\Node\" -Force
```

**Step 3: Restore SQLite database:**

```powershell
New-Item -ItemType Directory -Force -Path "C:\EdgeGuard\Node\persistence"
Copy-Item "C:\Backups\EdgeGuard\Node\edge-node-20250101_020000.db" `
          "C:\EdgeGuard\Node\persistence\edge-node.db"
```

**Step 4: (Re)install and start the Windows Service:**

```powershell
New-Service -Name "EdgeGuardNode" `
    -BinaryPathName "C:\EdgeGuard\Node\Dicom.Edge.Node.exe" `
    -StartupType Automatic `
    -DisplayName "EdgeGuard Edge Node"
Start-Service EdgeGuardNode
Invoke-RestMethod http://localhost:5001/health
```

The node will reconnect to the Hub automatically using its existing NodeId and certificate.

### If on Linux (alternative recovery)

```bash
# Restore Hub
tar -xzf /var/backups/edgeguard/hub/config-20250101.tar.gz -C /
tar -xzf /var/backups/edgeguard/hub/certs-20250101.tar.gz -C /
psql -U postgres -c "CREATE USER edgeguard WITH PASSWORD 'your_password';"
psql -U postgres -c "CREATE DATABASE edgeguard_hub OWNER edgeguard;"
PGPASSWORD=your_password pg_restore --host=localhost --port=5432 \
  --username=edgeguard --dbname=edgeguard_hub --no-owner --role=edgeguard \
  /var/backups/edgeguard/hub/edgeguard_hub_20250101_020000.dump
dotnet publish src/backend/Dicom.Edge.Hub.Api --configuration Release \
  --output /opt/edgeguard/hub
sudo systemctl start edgeguard-hub
curl https://your-hub-domain.example.com/health

# Restore Node
tar -xzf /var/backups/edgeguard/node/config-20250101.tar.gz -C /
mkdir -p /opt/edgeguard/node/persistence
cp /var/backups/edgeguard/node/edge-node-20250101_020000.db \
   /opt/edgeguard/node/persistence/edge-node.db
sudo systemctl start edgeguard-node
curl http://localhost:5001/health
```

---

## Backup Verification

Test your backup procedure monthly. Record results in your operations log.

### PostgreSQL backup verification (PowerShell, Windows — primary)

```powershell
$psql      = "C:\Program Files\PostgreSQL\16\bin\psql.exe"
$pgRestore = "C:\Program Files\PostgreSQL\16\bin\pg_restore.exe"

& $psql -U postgres -c "CREATE DATABASE edgeguard_hub_verify;"

& $pgRestore --dbname=edgeguard_hub_verify --username=postgres --no-owner `
    "C:\Backups\EdgeGuard\Hub\edgeguard_hub_20250101_020000.dump"

# Spot check
& $psql -U postgres -d edgeguard_hub_verify -c 'SELECT COUNT(*) FROM "Studies";'
& $psql -U postgres -d edgeguard_hub_verify -c 'SELECT COUNT(*) FROM "Patients";'
& $psql -U postgres -d edgeguard_hub_verify -c 'SELECT COUNT(*) FROM "Nodes";'

& $psql -U postgres -c "DROP DATABASE edgeguard_hub_verify;"
```

### SQLite backup verification (PowerShell, Windows — primary)

```powershell
sqlite3 "C:\Backups\EdgeGuard\Node\edge-node-20250101_020000.db" `
    "PRAGMA integrity_check; SELECT COUNT(*) FROM DicomRoutingRules;"
```

### If on Linux (alternative)

```bash
psql -U postgres -c "CREATE DATABASE edgeguard_hub_verify;"
pg_restore --dbname=edgeguard_hub_verify --username=postgres --no-owner \
  /var/backups/edgeguard/hub/edgeguard_hub_20250101_020000.dump
psql -U postgres -d edgeguard_hub_verify -c 'SELECT COUNT(*) FROM "Studies";'
psql -U postgres -c "DROP DATABASE edgeguard_hub_verify;"

sqlite3 /var/backups/edgeguard/node/edge-node-20250101_020000.db \
  "PRAGMA integrity_check; SELECT COUNT(*) FROM DicomRoutingRules;"
```
