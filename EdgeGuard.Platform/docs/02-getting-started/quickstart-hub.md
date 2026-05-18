# Quickstart — Hub Central

This guide walks you through a complete Hub setup from a fresh clone to a running system. Expected time: **under 15 minutes** on a machine that meets the [prerequisites](prerequisites.md).

---

## Before You Begin

Verify your environment:

```bash
dotnet --version    # must be 10.x.x
node --version      # must be v22.x.x or later
ng version          # Angular CLI 19.x
dotnet ef --version # EF Core CLI 10.x
```

Ensure PostgreSQL 16+ is running and you have a database ready:

```sql
-- Run as a PostgreSQL superuser
CREATE USER edgeguard WITH PASSWORD 'your-strong-password';
CREATE DATABASE edgeguard_hub OWNER edgeguard;
```

---

## Step 1 — Clone the Repository

```bash
git clone https://github.com/your-org/EdgeGuard.Platform.git
cd EdgeGuard.Platform/EdgeGuard.Platform
```

> **Note:** If you received the source as an archive instead of a Git repository, extract it and navigate to the root directory containing the `src/` folder.

---

## Step 2 — Set the Database Connection String

The Hub requires a PostgreSQL connection string. Set it as an environment variable so that it takes precedence over any `appsettings.json` value.

**Linux / macOS:**
```bash
export HUB_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=your-strong-password"
```

**Windows (PowerShell):**
```powershell
$env:HUB_DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=your-strong-password"
```

**Windows (Command Prompt):**
```cmd
set HUB_DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=your-strong-password
```

> **Troubleshooting:** If the Hub fails to start with `Failed to connect to database`, double-check that PostgreSQL is accepting connections on port 5432 and that the user credentials are correct. You can test with:
> ```bash
> psql "Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard" -c "SELECT 1;"
> ```

---

## Step 3 — Run Database Migrations

Apply EF Core migrations to create the Hub schema:

```bash
dotnet ef database update \
  --project src/backend/Dicom.Edge.Hub.Persistence \
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

On Windows:
```powershell
dotnet ef database update `
  --project src/backend/Dicom.Edge.Hub.Persistence `
  --startup-project src/backend/Dicom.Edge.Hub.Api
```

Expected output ends with:
```
Applying migration '20250201000001_InitialCreate'.
...
Done.
```

> **Troubleshooting:** If you receive `No migrations were applied`, the database may already be up to date. Run `dotnet ef migrations list --project ... --startup-project ...` to confirm all migrations have the `[X]` applied marker.

> **Troubleshooting:** If you receive a permissions error (`permission denied for table`), ensure the PostgreSQL user has the required `CREATE` and `ALTER` privileges on the target database.

---

## Step 4 — Build and Run the Hub API

```bash
dotnet run --project src/backend/Dicom.Edge.Hub.Api
```

The first time the Hub starts with an empty `Users` table, it generates a **bootstrap token** and prints it to the console. Look for a line similar to:

```
[INF] Bootstrap token generated. Use this token to create the first administrator account.
[INF] Bootstrap token: eyJhb...  (valid for 1 hour)
```

**Copy this token — it will not be shown again.**

Successful startup produces output like:

```
[INF] Now listening on: http://localhost:5000
[INF] Now listening on: https://localhost:5001
[INF] HL7 MLLP listener started on TCP port 8001
[INF] Application started. Press Ctrl+C to shut down.
```

> **Troubleshooting:** If port 5000 is already in use, override it:
> ```bash
> dotnet run --project src/backend/Dicom.Edge.Hub.Api --urls "http://localhost:5100"
> ```

> **Troubleshooting:** If the HL7 listener fails to bind port 8001, check whether another process holds the port:
> ```bash
> # Linux
> ss -tlnp | grep 8001
> # Windows
> netstat -ano | findstr :8001
> ```
> Alternatively, disable the HL7 listener temporarily: set `Hl7Listener__Enabled=false` in your environment.

---

## Step 5 — Access the SPA and Use the Bootstrap Token

Open your browser and navigate to:

```
http://localhost:5000
```

The Angular SPA loads and presents the **Bootstrap Login** screen (visible only when no admin account exists). Enter the bootstrap token you copied in Step 4.

> **Troubleshooting:** If the browser shows a blank page, the SPA may not have been published to `wwwroot`. Build and publish first:
> ```bash
> cd src/frontend/dicomedge-ui
> npm install
> ng build --configuration production
> # Output is automatically copied to ../Dicom.Edge.Hub.Api/wwwroot by the build pipeline
> ```

---

## Step 6 — Create the First Administrator Account

After authenticating with the bootstrap token, the SPA redirects you to the **Create Administrator** wizard:

1. Enter a **username** (e.g., `admin`).
2. Enter a **password** that satisfies the complexity rules (min. 12 characters, upper, lower, digit, symbol).
3. Enter your **email address**.
4. Click **Create Administrator**.

The bootstrap token is immediately invalidated. You are redirected to the main dashboard and logged in as the new administrator.

**Next steps from the dashboard:**
- Navigate to **Nodes** to register your first Edge Node.
- Navigate to **PACS Destinations** to add target PACS servers.
- Navigate to **Routing Rules** to configure study routing.

---

## Production Hardening Checklist

Before exposing the Hub to a production network, complete the following:

- [ ] Replace the self-signed development certificate with a trusted TLS certificate and configure `--urls https://0.0.0.0:5001`.
- [ ] Set `Jwt__Secret` to a cryptographically random 64-character string (not the development default).
- [ ] Set `Diagnostics__Redaction__Mode=Relaxed` (or `Strict` if required by your compliance policy).
- [ ] Configure `Cors__AllowedOrigins` to list only your SPA origin(s).
- [ ] Set up PostgreSQL backups (pg_dump or continuous WAL archiving).
- [ ] Configure a reverse proxy (nginx, IIS, Caddy) for TLS termination.
- [ ] Review all environment variables in the [Environment Variables](environment-variables.md) reference.

---

## Summary

| Step | Command / Action |
|------|-----------------|
| 1 | `git clone` the repository |
| 2 | Set `HUB_DB_CONNECTION_STRING` environment variable |
| 3 | `dotnet ef database update` to apply migrations |
| 4 | `dotnet run` Hub API; copy bootstrap token from logs |
| 5 | Open `http://localhost:5000`; enter bootstrap token |
| 6 | Create the first administrator account in the UI |
