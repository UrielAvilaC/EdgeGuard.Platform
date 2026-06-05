# Database Migrations Guide

EdgeGuard Platform uses Entity Framework Core for Hub database schema management with PostgreSQL 16.

---

## Table of Contents

1. [Auto-Apply on Startup](#auto-apply-on-startup)
2. [Migration History](#migration-history)
3. [Manual Apply Commands](#manual-apply-commands)
4. [Adding a New Migration](#adding-a-new-migration)
5. [Rolling Back a Migration](#rolling-back-a-migration)
6. [Production Policy](#production-policy)
7. [Backup Before Migration](#backup-before-migration)

---

## Auto-Apply on Startup

By default, the Hub applies all pending EF Core migrations automatically during startup. This is handled via `app.Services.MigrateHubAsync()` called in `Program.cs`.

```
[INF] Applying database migrations...
[INF] Migration 'InitialMigrationHub' already applied — skipping.
[INF] Applying migration 'AddNodeTelemetryRecords'...
[INF] Applying migration 'AddNodeDicomRoutingRules'...
[INF] All migrations applied successfully.
[INF] Hub startup complete.
```

**No manual step is required** for standard deployments. Auto-migration is safe for development and staging environments. See [Production Policy](#production-policy) for production guidance.

---

## Migration History

| # | Migration Name | Applied | Changes |
|---|----------------|---------|---------|
| 1 | `InitialMigrationHub` | v1.0.0 | Creates all core tables: Patients, Studies, StudySeries, StudyStatusAudits, Nodes, PacsServers, Users |
| 2 | `AddHl7MessageStudyDate` | v1.1.0 | Adds `StudyDate` column to Hl7Messages table |
| 3 | `AddHl7MessageModalityProcedure` | v1.1.0 | Adds `Modality` and `ProcedureDescription` columns to Hl7Messages |
| 4 | `AddHl7MessageProcedureId` | v1.2.0 | Adds `ProcedureId` column to Hl7Messages; adds index on ProcedureId |
| 5 | `AddNodeTelemetryRecords` | v1.3.0 | Creates NodeTelemetryRecords table for node heartbeat and metrics storage |
| 6 | `AddNodeDicomRoutingRules` | v1.4.0 | Creates NodeDicomRoutingRules table; adds FK to Nodes |
| 7 | `AddHl7MrgObxSupport` | v1.5.0 | Adds `MergePatientId` column (MRG segment support) and `ObxValueType` column (OBX segment support) to Hl7Messages |

---

## Manual Apply Commands

Use these when auto-apply is disabled or when you need precise control over which migrations are applied.

### Prerequisites

```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Check pending migrations

```bash
dotnet ef migrations list \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

Output:
```
20240101000000_InitialMigrationHub (Applied)
20240215000000_AddHl7MessageStudyDate (Applied)
20240301000000_AddHl7MessageModalityProcedure (Pending)
```

### Apply all pending migrations

```bash
dotnet ef database update \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

### Apply migrations up to a specific migration

```bash
dotnet ef database update AddHl7MessageProcedureId \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

---

## Adding a New Migration

When you modify an EF Core entity or `DbContext`, generate a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

**Naming convention:** Use PascalCase, descriptive names that describe the schema change.

Examples:
- `AddStudyAccessionNumber`
- `AddPacsServerPriority`
- `CreateAuditLogTable`

After generating, review the migration file in `src/backend/Dicom.Edge.Hub.Persistence/Migrations/` before committing. Ensure:

- The `Up()` method accurately reflects the intended change.
- The `Down()` method correctly reverses the change.
- No unintended table drops or column renames are included.

---

## Rolling Back a Migration

To undo the most recently applied migration, specify the previous migration name:

### Rollback the last migration

```bash
# Identify the migration before the one you want to undo
dotnet ef migrations list \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api

# Roll back to a specific point
dotnet ef database update AddHl7MessageProcedureId \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

This reverts the database to the state after `AddHl7MessageProcedureId` was applied, undoing all migrations that came after it.

### Remove the migration file (if not yet committed)

```bash
dotnet ef migrations remove \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

> **Warning:** `migrations remove` deletes the migration `.cs` file. Only use this for migrations that have NOT been applied to any database (i.e., local development only). Never remove a migration that has been applied to a shared or production database.

---

## Production Policy

> **IMPORTANT:** Never apply database migrations to a production database without a current backup.

Recommended production migration workflow:

1. **Take a full database backup** (see [Backup Before Migration](#backup-before-migration)).
2. **Apply in a maintenance window** when the Hub is stopped or in read-only mode.
3. **Run migrations against a staging database first** and verify application behavior.
4. **Apply to production** using the manual `dotnet ef database update` command.
5. **Start the application** and verify via `/health` that all checks pass.
6. **Retain the backup** for at least 7 days post-migration.

### Disabling auto-migration in production

To disable auto-apply and require manual migration control, set in `appsettings.Production.json`:

```json
{
  "Database": {
    "AutoMigrate": false
  }
}
```

When disabled, the Hub logs a warning at startup if pending migrations are detected and continues without applying them. The application will fail if the schema is incompatible.

---

## Backup Before Migration

### Full backup with pg_dump

```bash
# Set connection variables
export PGHOST=localhost
export PGPORT=5432
export PGUSER=edgeguard
export PGPASSWORD=your_password

# Create timestamped backup
pg_dump \
  --format=custom \
  --compress=9 \
  --file="/backups/edgeguard_hub_$(date +%Y%m%d_%H%M%S).dump" \
  edgeguard_hub

echo "Backup complete: $(ls -lh /backups/edgeguard_hub_*.dump | tail -1)"
```

### Verify the backup is readable

```bash
pg_restore --list /backups/edgeguard_hub_20250101_120000.dump | head -20
```

### Restore from backup (if migration fails)

```bash
# Drop and recreate the database
psql -U edgeguard -c "DROP DATABASE edgeguard_hub;"
psql -U edgeguard -c "CREATE DATABASE edgeguard_hub;"

# Restore
pg_restore \
  --dbname=edgeguard_hub \
  --username=edgeguard \
  --no-owner \
  /backups/edgeguard_hub_20250101_120000.dump

echo "Restore complete."
```

See [backup-recovery.md](./backup-recovery.md) for the full backup and recovery runbook.
