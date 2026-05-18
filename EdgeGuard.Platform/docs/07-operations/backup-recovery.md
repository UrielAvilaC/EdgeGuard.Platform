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

| Component | Location | Backup required | Frequency |
|-----------|----------|-----------------|-----------|
| Hub database (PostgreSQL) | Hub server | Yes — critical | Daily |
| Node database (SQLite) | Each node | Yes — recommended | Daily |
| Hub appsettings | Hub server `/etc/edgeguard/hub.env`, `appsettings.Production.json` | Yes | On change |
| Node appsettings | Each node `appsettings.Production.json` | Yes | On change |
| SSL/TLS certificates | Hub server | Yes | On renewal |
| DICOM temp files (`./data/`) | Each node | No — transient | N/A |
| Log files (`./logs/`) | Hub and Nodes | Optional | Offload to SIEM |

---

## RPO and RTO Targets

| Metric | Target | Achieved by |
|--------|--------|-------------|
| Recovery Point Objective (RPO) | 24 hours maximum data loss | Daily database backup |
| Recovery Time Objective (RTO) | < 1 hour to restore service | Documented recovery procedure |

For higher availability requirements (RPO < 1h), consider PostgreSQL streaming replication to a standby server.

---

## PostgreSQL Backup (Hub)

### Daily backup script

Create `/opt/edgeguard/scripts/backup-hub-db.sh`:

```bash
#!/bin/bash
set -euo pipefail

BACKUP_DIR="/backups/edgeguard/hub"
DB_NAME="edgeguard_hub"
DB_USER="edgeguard"
RETENTION_DAYS=14

mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.dump"

echo "[$(date -Iseconds)] Starting PostgreSQL backup: $BACKUP_FILE"

PGPASSWORD="$POSTGRES_PASSWORD" pg_dump \
  --host=localhost \
  --port=5432 \
  --username="$DB_USER" \
  --format=custom \
  --compress=9 \
  --file="$BACKUP_FILE" \
  "$DB_NAME"

SIZE=$(du -sh "$BACKUP_FILE" | cut -f1)
echo "[$(date -Iseconds)] Backup complete. Size: $SIZE"

# Remove backups older than retention period
find "$BACKUP_DIR" -name "*.dump" -mtime +"$RETENTION_DAYS" -delete
echo "[$(date -Iseconds)] Cleaned up backups older than $RETENTION_DAYS days."
```

Make executable and schedule:

```bash
chmod +x /opt/edgeguard/scripts/backup-hub-db.sh

# Add to crontab — run daily at 02:00 AM
echo "0 2 * * * edgeguard POSTGRES_PASSWORD=your_password /opt/edgeguard/scripts/backup-hub-db.sh >> /var/log/edgeguard-backup.log 2>&1" \
  | sudo tee -a /etc/cron.d/edgeguard-backup
```

### Manual on-demand backup

```bash
PGPASSWORD=your_password pg_dump \
  --host=localhost \
  --port=5432 \
  --username=edgeguard \
  --format=custom \
  --compress=9 \
  --file="/backups/edgeguard/hub/manual_$(date +%Y%m%d_%H%M%S).dump" \
  edgeguard_hub
```

### Backup to remote storage (S3-compatible)

```bash
# After pg_dump completes, upload to S3
aws s3 cp "$BACKUP_FILE" "s3://your-backup-bucket/edgeguard/hub/" \
  --storage-class STANDARD_IA \
  --sse AES256
```

---

## SQLite Backup (Edge Nodes)

Each node uses a SQLite database at `./persistence/edge-node.db`. SQLite supports online backup via the `.backup` command (WAL-aware, safe while the node is running) or a simple file copy when the node is stopped.

### Option A: Online backup (node running)

```bash
# Uses SQLite's .backup command via sqlite3 CLI
sqlite3 /opt/edgeguard/node/persistence/edge-node.db \
  ".backup '/backups/edgeguard/node/edge-node-$(date +%Y%m%d_%H%M%S).db'"
```

This is WAL-safe and does not require stopping the node.

### Option B: File copy (node stopped)

```bash
# Stop the node
sudo systemctl stop edgeguard-node

# Copy the database file
cp /opt/edgeguard/node/persistence/edge-node.db \
   /backups/edgeguard/node/edge-node-$(date +%Y%m%d_%H%M%S).db

# Start the node
sudo systemctl start edgeguard-node
```

### Windows node backup (PowerShell)

```powershell
# Online backup
sqlite3 C:\EdgeGuard\Node\persistence\edge-node.db `
  ".backup 'C:\Backups\EdgeGuard\Node\edge-node-$(Get-Date -Format yyyyMMdd_HHmmss).db'"
```

---

## Configuration and Certificates

### Hub configuration files

```bash
# Backup environment and settings files
tar -czf /backups/edgeguard/hub/config-$(date +%Y%m%d).tar.gz \
  /etc/edgeguard/hub.env \
  /opt/edgeguard/hub/appsettings.json \
  /opt/edgeguard/hub/appsettings.Production.json

# SSL certificates
tar -czf /backups/edgeguard/hub/certs-$(date +%Y%m%d).tar.gz \
  /etc/ssl/certs/edgeguard.crt \
  /etc/ssl/private/edgeguard.key
```

> **Security:** Store certificate backups in a separate, access-controlled location. Never commit certificate files to version control.

### Node configuration files

```bash
tar -czf /backups/edgeguard/node/config-$(date +%Y%m%d).tar.gz \
  /opt/edgeguard/node/appsettings.json \
  /opt/edgeguard/node/appsettings.Production.json
```

---

## Transient Data (DICOM temp files)

The `./data/` directory on each node holds DICOM files currently in transit (received from modality, awaiting forwarding to PACS). This data is **transient and not included in backup scope**.

- Files in `./data/` are removed automatically after successful PACS forwarding.
- If a node fails with studies in `./data/`, those studies must be re-sent from the originating modality.
- This is acceptable within the defined RPO — modalities can be asked to re-send if needed.

For environments where re-send from modality is not practical, consider mounting `./data/` on network-attached storage with its own redundancy.

---

## Recovery Procedure

### Full Hub recovery after server loss

**Step 1: Provision replacement server**

Install OS, .NET Runtime, PostgreSQL, and restore configuration files:

```bash
# Restore config
tar -xzf /backups/edgeguard/hub/config-20250101.tar.gz -C /
tar -xzf /backups/edgeguard/hub/certs-20250101.tar.gz -C /
```

**Step 2: Restore the PostgreSQL database**

```bash
# Create database
psql -U postgres -c "CREATE USER edgeguard WITH PASSWORD 'your_password';"
psql -U postgres -c "CREATE DATABASE edgeguard_hub OWNER edgeguard;"

# Restore from backup
PGPASSWORD=your_password pg_restore \
  --host=localhost \
  --port=5432 \
  --username=edgeguard \
  --dbname=edgeguard_hub \
  --no-owner \
  --role=edgeguard \
  /backups/edgeguard/hub/edgeguard_hub_20250101_020000.dump

echo "Restore complete."
```

**Step 3: Deploy the Hub application**

```bash
dotnet publish src/backend/Dicom.Edge.Hub.Api \
  --configuration Release \
  --output /opt/edgeguard/hub

sudo systemctl start edgeguard-hub
```

**Step 4: Verify health**

```bash
curl https://your-hub-domain.example.com/health
# Expected: {"status":"Healthy",...}
```

**Step 5: Verify nodes reconnect**

In the Hub UI, check that all nodes show **Connected** status within 2–3 minutes of Hub restart.

### Node recovery after hardware failure

**Step 1: Install .NET Runtime and deploy node application.**

**Step 2: Restore configuration:**

```bash
tar -xzf /backups/edgeguard/node/config-20250101.tar.gz -C /
```

**Step 3: Restore SQLite database:**

```bash
mkdir -p /opt/edgeguard/node/persistence
cp /backups/edgeguard/node/edge-node-20250101_020000.db \
   /opt/edgeguard/node/persistence/edge-node.db
chown edgeguard-node:edgeguard-node /opt/edgeguard/node/persistence/edge-node.db
```

**Step 4: Start and verify:**

```bash
sudo systemctl start edgeguard-node
curl http://localhost:5001/health
```

The node will reconnect to the Hub automatically using its existing NodeId and certificate.

---

## Backup Verification

Test your backup procedure monthly. Record results in your operations log.

### PostgreSQL backup verification

```bash
# Restore to a test database
psql -U postgres -c "CREATE DATABASE edgeguard_hub_verify;"

pg_restore \
  --dbname=edgeguard_hub_verify \
  --username=postgres \
  --no-owner \
  /backups/edgeguard/hub/edgeguard_hub_20250101_020000.dump

# Spot check: count key records
psql -U postgres -d edgeguard_hub_verify -c "SELECT COUNT(*) FROM \"Studies\";"
psql -U postgres -d edgeguard_hub_verify -c "SELECT COUNT(*) FROM \"Patients\";"
psql -U postgres -d edgeguard_hub_verify -c "SELECT COUNT(*) FROM \"Nodes\";"

# Clean up
psql -U postgres -c "DROP DATABASE edgeguard_hub_verify;"
```

### SQLite backup verification

```bash
sqlite3 /backups/edgeguard/node/edge-node-20250101_020000.db \
  "PRAGMA integrity_check; SELECT COUNT(*) FROM DicomRoutingRules;"
```
