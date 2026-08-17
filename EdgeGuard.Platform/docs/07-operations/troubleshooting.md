# Troubleshooting Guide

This guide covers the most common operational issues with EdgeGuard Platform. Each scenario includes symptoms, root cause, and resolution steps.

> **Platform terminology used below:**
> - **Hub:** runs under the IIS App Pool `EdgeGuardHub` (in-process AspNetCoreModuleV2). When a runbook says "restart the Hub", that means `Restart-WebAppPool -Name EdgeGuardHub` (or `iisreset` as a heavy-handed alternative).
> - **Edge Node:** runs as the Windows Service `EdgeGuardNode`. "Restart the node" means `Restart-Service EdgeGuardNode`.
> - **Logs:** Hub logs live under `C:\inetpub\EdgeGuard\Hub\logs\`; Node logs under `C:\EdgeGuard\Node\logs\`. Service startup failures also surface in **Windows Event Viewer → Windows Logs → Application** and IIS failed-request tracing.
> Linux/Docker equivalents (`systemctl`, `journalctl`, `/opt/edgeguard/...`) are shown as alternatives where relevant.

---

## Quick Reference

| # | Problem | See |
|---|---------|-----|
| 1 | Hub won't start | [Hub Won't Start](#1-hub-wont-start) |
| 2 | DICOM association rejected | [DICOM Association Rejected](#2-dicom-association-rejected) |
| 3 | HL7 messages not arriving | [HL7 Messages Not Arriving](#3-hl7-messages-not-arriving) |
| 4 | HL7 validation failure | [HL7 Validation Failure](#4-hl7-validation-failure) |
| 5 | Study stuck in Sending | [Study Stuck in Sending](#5-study-stuck-in-sending) |
| 6 | Node not connecting to Hub | [Node Not Connecting to Hub](#6-node-not-connecting-to-hub) |
| 7 | JWT 401 Unauthorized errors | [JWT 401 Errors](#7-jwt-401-errors) |
| 8 | Rate limit 429 errors | [Rate Limit 429 Errors](#8-rate-limit-429-errors) |
| 9 | EF migration failure | [EF Migration Failure](#9-ef-migration-failure) |
| 10 | SPA blank page | [SPA Blank Page](#10-spa-blank-page) |
| 11 | SignalR disconnecting | [SignalR Disconnecting](#11-signalr-disconnecting) |
| 12 | Patient merge not working | [Patient Merge Not Working](#12-patient-merge-not-working) |
| 13 | Image links not attached to study | [Image Links Not Attached](#13-image-links-not-attached) |
| 14 | MWL query returns empty | [MWL Query Returns Empty](#14-mwl-query-returns-empty) |
| 15 | High memory usage on Hub | [High Memory on Hub](#15-high-memory-on-hub) |

---

## 1. Hub Won't Start

**Symptoms**
- IIS App Pool `EdgeGuardHub` is **Stopped** (auto-disabled after rapid-fail) — visible in IIS Manager
- Browser receives HTTP 502.5 (ANCM Out-of-Process Startup Failure) or 500.30 (In-Process Startup Failure)
- Windows Event Log (Application) shows `IIS AspNetCore Module V2` error events
- `Get-Service W3SVC` shows running, but `Get-WebAppPoolState EdgeGuardHub` shows `Stopped`

**Cause**

The most common cause is a missing or invalid `HUB_DB_CONNECTION_STRING` environment variable on the IIS App Pool.

**Diagnosis (Windows — primary)**

```powershell
# Inspect the most recent ASP.NET Core / IIS errors
Get-EventLog -LogName Application -Source "IIS AspNetCore Module V2" -Newest 20 |
    Format-List TimeGenerated, EntryType, Message

# Inspect stdout logs (enabled via web.config <aspNetCore stdoutLogEnabled="true">)
Get-ChildItem "C:\inetpub\EdgeGuard\Hub\logs\stdout*.log" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1 |
    Get-Content -Tail 80

# IIS failed request tracing
Get-ChildItem "C:\inetpub\logs\FailedReqLogFiles\W3SVC*"
```

Look for:
```
[FTL] Critical startup error: HUB_DB_CONNECTION_STRING is not set or empty.
```
or:
```
[FTL] Npgsql.NpgsqlException: Connection refused (localhost:5432)
```

**Resolution (Windows — primary)**

1. Verify the env var is set on the App Pool (IIS Manager → Application Pools → EdgeGuardHub → Advanced Settings → Environment Variables), or via PowerShell:
```powershell
Get-ItemProperty "IIS:\AppPools\EdgeGuardHub" -Name "environmentVariables.collection"
```

2. Verify PostgreSQL is running and reachable:
```powershell
Test-NetConnection -ComputerName localhost -Port 5432
& "C:\Program Files\PostgreSQL\16\bin\pg_isready.exe" -h localhost -p 5432 -U edgeguard -d edgeguard_hub
```

3. Set the variable and recycle the App Pool:
```powershell
$pool = "IIS:\AppPools\EdgeGuardHub"
$envColl = @{ name = "HUB_DB_CONNECTION_STRING"; value = "Host=localhost;Port=5432;Database=edgeguard_hub;Username=edgeguard;Password=..." }
Add-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter "system.applicationHost/applicationPools/add[@name='EdgeGuardHub']/environmentVariables" `
    -Name "." -Value $envColl

Restart-WebAppPool -Name EdgeGuardHub
```

**If on Linux (alternative)**

```bash
sudo journalctl -u edgeguard-hub -n 50 --no-pager
sudo cat /etc/edgeguard/hub.env | grep HUB_DB_CONNECTION_STRING
pg_isready -h localhost -p 5432 -U edgeguard -d edgeguard_hub
docker compose ps; docker compose logs postgres   # if using Docker
sudo systemctl restart edgeguard-hub
```

---

## 2. DICOM Association Rejected

**Symptoms**
- Modality reports "Association rejected" or "C-STORE failed"
- Studies are not received on the node
- Node logs show association rejection

**Cause**

AE Title mismatch, port not reachable, or the modality's calling AE Title is not in the allowed list.

**Diagnosis (Windows — primary)**

```powershell
# Confirm the Windows Service is running and listening on 11112
Get-Service EdgeGuardNode
Get-NetTCPConnection -LocalPort 11112 -State Listen

# Check node DICOM listener is running
Invoke-RestMethod http://localhost:5001/health

# Check node logs for rejection details
Get-Content "C:\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log" -Tail 100 |
    Select-String -Pattern "association"
```

**Diagnosis (Linux, alternative)**

```bash
curl http://localhost:5001/health
tail -100 /opt/edgeguard/node/logs/node-$(date +%Y%m%d).log | grep -i "association"
```

Expected rejection log:
```
[WRN] DICOM association rejected: CallingAE=MODALITY_AE, CalledAE=WRONG_AE_TITLE
  Reason: Called AE Title not recognized. Configured AE: EDGEGUARD_SITE_A
```

**Resolution**

1. Verify the node's AE Title in `appsettings.json`:
```json
{ "Dicom": { "AeTitle": "EDGEGUARD_SITE_A" } }
```

2. Ensure the modality is configured to send to exactly this AE Title (case-sensitive, max 16 characters, no leading/trailing spaces).

3. Verify the DICOM port is reachable from the modality:

   **PowerShell (Windows):**
   ```powershell
   Test-NetConnection -ComputerName <node-ip> -Port 11112
   ```

   **Bash (Linux):**
   ```bash
   nc -zv <node-ip> 11112
   ```

4. Check firewall rules allow the modality's IP on the DICOM port. On Windows, inspect the rule with:
   ```powershell
   Get-NetFirewallRule -DisplayName "EdgeGuard*" | Get-NetFirewallPortFilter
   ```

5. If the node restricts calling AE Titles, add the modality AE to the allowed list in the Hub UI under **Nodes → [Node] → Allowed AE Titles**.

**Fastest path: read the association's own log file**

The node writes one file per association (see
[monitoring.md → Per-Association DICOM Logs](monitoring.md#per-association-dicom-logs)),
including the rejected ones. It contains the negotiation and the exact rejection reason for
that single association, with no interleaving from other modalities:

```powershell
# The most recent associations from a given modality, newest first
Get-ChildItem "C:\EdgeGuard\Node\logs\associations" -Recurse -Filter "*CT-SIEMENS*.log" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 5

# Read the last one end to end
Get-ChildItem "C:\EdgeGuard\Node\logs\associations" -Recurse -Filter "*CT-SIEMENS*.log" |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content

# Or list every association that ended badly today
Get-ChildItem "C:\EdgeGuard\Node\logs\associations\$(Get-Date -Format yyyy-MM-dd)" |
    Select-String -Pattern "status=(Rejected|Aborted)"
```

This is also the file to send to the modality vendor. If the folder is empty, check that
`Diagnostics:File:PerAssociation:Enabled` is `true` (or `diagnostics.assoc_log_enabled` on the
Hub) — and note the setting applies from the **next** association onward.

---

## 3. HL7 Messages Not Arriving

**Symptoms**
- HIS/RIS reports messages sent but no studies appear in EdgeGuard
- No entries appear in Hub HL7 message log
- HIS/RIS receives timeout or connection refused

**Cause**

MLLP port (8001) is blocked by firewall, or the sending system is not using proper MLLP framing.

**Diagnosis (Windows — primary)**

```powershell
# Confirm the MLLP listener inside w3wp.exe is bound on :8001
Get-NetTCPConnection -LocalPort 8001 -State Listen

# Test MLLP port connectivity from the HIS/RIS host
Test-NetConnection -ComputerName <hub-ip> -Port 8001

# Check Hub logs
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Tail 200 |
    Select-String -Pattern "mllp|hl7|8001"
```

**Diagnosis (Linux, alternative)**

```bash
nc -zv <hub-ip> 8001
grep -i "mllp\|hl7\|8001" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | tail -20
```

**Resolution**

1. Open port 8001 TCP from the HIS/RIS network segment to the Hub host.

2. Verify the HIS/RIS is using MLLP framing (required — raw TCP without framing will not work):
   - Start byte: `0x0B` (vertical tab)
   - End bytes: `0x1C 0x0D` (file separator + carriage return)

3. Use HAPI TestPanel to send a test message directly and confirm it arrives (see [hl7-integration-guide.md](../08-integrations/hl7-integration-guide.md)).

4. Check the Hub HL7 listener health:

   **PowerShell (Windows):**
   ```powershell
   Invoke-RestMethod https://hub.your-org.local/health
   # Look for "hl7-listener" check
   ```

   **Bash (Linux):**
   ```bash
   curl http://localhost:5000/health
   ```

> The MLLP listener is a raw-socket `IHostedService` running inside the same `w3wp.exe` worker as the REST API. If the App Pool `EdgeGuardHub` is recycling or its `startMode` is not `AlwaysRunning` / `idleTimeout` is not `00:00:00`, the listener can become unreachable. See [deployment-hub.md](./deployment-hub.md).

---

## 4. HL7 Validation Failure

**Symptoms**
- MLLP connection succeeds, Hub sends NACK (`MSA-1 = AE`)
- HL7 message log shows validation errors
- Study not created in EdgeGuard

**Cause**

Unsupported trigger event, missing required fields, or malformed segment.

**Diagnosis**

Check Hub logs for the specific validation error:

```
[WRN] HL7 validation failed: MessageControlId=MSG001, MessageType=ADT^A05
  Error: Unsupported trigger event A05. Supported events: A01, A40.
```

```
[WRN] HL7 validation failed: MessageControlId=MSG002, MessageType=ORM^O01
  Error: Required field PID-3 (Patient ID) is missing or empty.
```

**Resolution**

1. Verify the message type is supported:

   | Message Type | Trigger Events |
   |-------------|----------------|
   | ADT | A01 (admit), A40 (patient merge) |
   | ORM | O01 (order) |
   | ORU | R01 (observation result) |

2. Verify required fields are present. See [hl7-integration-guide.md](../08-integrations/hl7-integration-guide.md) for per-message required field tables.

3. Check the NACK response returned by the Hub — the MSA-3 (Text Message) field contains the specific error description.

---

## 5. Study Stuck in Sending

**Symptoms**
- Study status shows `Sending` for an extended period (>10 minutes)
- PACS does not receive the study
- Hub logs show C-STORE SCU failures

**Cause**

PACS server unreachable, wrong AE Title configuration, or firewall blocking the DICOM port.

**Diagnosis (Windows — primary)**

```powershell
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Tail 300 |
    Select-String -Pattern "c-store|pacs|send"
```

**Diagnosis (Linux, alternative)**

```bash
grep -i "c-store\|pacs\|send" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | tail -30
```

Expected error:
```
[WRN] DICOM C-STORE SCU failed: PACS [OrthanProd | AE=ORTHANC_PROD]
  Host=pacs.internal.example.com, Port=11112
  Error=Association request timeout after 30s
```

**Resolution**

1. Use the C-ECHO test in the Hub UI: **Settings → PACS Servers → [PACS] → Test Connectivity**.

2. Manually test from the Hub server:
```bash
# Requires dcmtk or similar DICOM toolkit
echoscu pacs.internal.example.com 11112 -aec ORTHANC_PROD -aet EDGEGUARD_HUB
```

3. Verify PACS hostname/IP, port, and AE Title in Hub UI.

4. Check the firewall allows outbound TCP from Hub to PACS on the configured port.

5. On PACS side, verify that the Hub's AE Title is in the PACS permitted callers list.

6. After fixing the PACS configuration, use **Studies → [Study] → Retry Send** in the Hub UI.

---

## 6. Node Not Connecting to Hub

**Symptoms**
- Node shows as `Offline` or `Unknown` in Hub UI
- Node logs show connection refused or authentication failure
- No configuration sync to node

**Cause**

Hub URL misconfigured, Hub not running, or registration key expired/invalid.

**Diagnosis (Windows — primary)**

```powershell
Get-Content "C:\EdgeGuard\Node\logs\node-$(Get-Date -Format yyyyMMdd).log" -Tail 50 |
    Select-String -Pattern "hub|connect|register"
```

**Diagnosis (Linux, alternative)**

```bash
tail -50 /opt/edgeguard/node/logs/node-$(date +%Y%m%d).log | grep -i "hub\|connect\|register"
```

Expected error:
```
[ERR] Failed to connect to Hub: https://wrong-hub-url.example.com/api/nodes/register
  Error=HttpRequestException: Name or service not known
```

**Resolution**

1. Verify Hub URL in `appsettings.Production.json`:
```json
{ "Node": { "HubBaseUrl": "https://your-hub-domain.example.com" } }
```

2. Test Hub reachability from the node:

   **PowerShell (Windows):**
   ```powershell
   Invoke-RestMethod https://your-hub-domain.example.com/health
   ```

   **Bash (Linux):**
   ```bash
   curl https://your-hub-domain.example.com/health
   ```

3. If registration key was regenerated in Hub UI, update `Node.RegistrationKey` in `C:\EdgeGuard\Node\appsettings.Production.json` and restart the Windows Service:
   ```powershell
   Restart-Service EdgeGuardNode
   ```

4. Check Hub is running:

   **PowerShell (on Hub server):**
   ```powershell
   Get-WebAppPoolState -Name EdgeGuardHub
   Get-Service W3SVC
   ```

   **Bash (Linux):**
   ```bash
   sudo systemctl status edgeguard-hub
   ```

---

## 7. JWT 401 Errors

**Symptoms**
- API calls return `401 Unauthorized`
- SPA shows "Session expired" or login redirect loop
- Automation scripts fail with 401

**Cause**

JWT token has expired (default 60-minute expiry) and has not been refreshed.

**Diagnosis**

Decode the token at [jwt.io](https://jwt.io) (do not use production tokens on public sites — use a local decoder) and check the `exp` claim.

Check Hub logs:
```
[WRN] JWT validation failed: token expired at 2025-01-15T14:30:00Z (current: 2025-01-15T15:32:00Z)
```

**Resolution**

1. For SPA users: log out and log back in. The SPA should handle token refresh automatically — if it does not, this is a session management issue (see [SignalR Disconnecting](#11-signalr-disconnecting)).

2. For API automation: implement token refresh using the `/api/auth/refresh` endpoint with the refresh token.

3. To extend default token expiry for automation scenarios:
```env
Jwt__ExpiryMinutes=480
```

4. Verify system clocks on client and Hub are synchronized (NTP). Clock drift can cause premature token rejection.

---

## 8. Rate Limit 429 Errors

**Symptoms**
- API returns `429 Too Many Requests`
- Response header `Retry-After: N` indicates wait time
- Integration scripts or dashboards experience intermittent failures

**Cause**

Request rate exceeds configured limits:
- Edge API: 100 requests/minute
- Main API: 200 requests/minute

**Diagnosis**

```
[WRN] Rate limit exceeded: ClientIP=10.0.1.50, Path=/api/studies,
  RequestCount=147, Limit=100, WindowSeconds=60
```

**Resolution**

1. Identify the client generating excess requests (check `ClientIP` in logs).

2. Add client-side rate limiting and retry with exponential backoff:
```csharp
// Example: Polly retry with jitter
var policy = Policy
    .Handle<HttpRequestException>()
    .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
        + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)));
```

3. For legitimate high-volume use cases, contact platform administrators to discuss rate limit adjustments.

4. Implement request caching on the client side for read-heavy endpoints (`/api/studies`, `/api/patients`).

---

## 9. EF Migration Failure

**Symptoms**
- Hub fails to start with a migration error
- Log shows `PgException` or `MigrationException`
- Database schema is partially updated

**Cause**

Database user lacks `CREATE TABLE`/`ALTER TABLE` privileges, or a migration was partially applied and left the schema in an inconsistent state.

**Diagnosis**

```
[FTL] Database migration failed: migration 'AddNodeDicomRoutingRules'
  Npgsql.PostgresException: permission denied for schema public
```

or:

```
[FTL] Database migration failed: column "RoutingPriority" of relation "NodeDicomRoutingRules" already exists
```

**Resolution**

**Missing privilege:**
```sql
-- Connect as superuser and grant privileges
GRANT CREATE, USAGE ON SCHEMA public TO edgeguard;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO edgeguard;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO edgeguard;
```

**Partially applied migration:**

1. Take a database backup first.
2. Check the `__EFMigrationsHistory` table to see which migrations are recorded as applied:
```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```
3. If the migration is listed but the schema changes are incomplete, manually remove the entry and re-apply:
```sql
DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20240601000000_AddNodeDicomRoutingRules';
```
4. Restart the Hub to trigger auto-migration.

See [database-migrations.md](./database-migrations.md) for full migration management guidance.

---

## 10. SPA Blank Page

**Symptoms**
- Navigating to Hub URL shows a blank white page
- Browser console shows `Cannot GET /` or 404 for `main.js`
- No Angular application loads

**Cause**

Angular production build artifacts are missing from `wwwroot`. The SPA was not built or the build output was not copied to the publish directory.

**Diagnosis**

```bash
# Check if wwwroot contains built Angular files
ls /opt/edgeguard/hub/wwwroot/
# Expected: index.html, main.js, polyfills.js, styles.css, assets/
# If empty or missing: build has not been run
```

**Resolution**

1. Build the Angular SPA:
```bash
cd src/frontend/dicomedge-ui
npm install
ng build --configuration production
```

2. Copy build output to Hub wwwroot:
```bash
cp -r dist/dicomedge-ui/browser/* /opt/edgeguard/hub/wwwroot/
```

3. In CI/CD, ensure the Angular build step runs before the .NET publish step and that artifacts are merged correctly.

---

## 11. SignalR Disconnecting

**Symptoms**
- Real-time study updates stop working in the SPA
- Browser console shows WebSocket errors or repeated reconnection attempts
- Hub logs show frequent SignalR connection/disconnection cycles

**Cause**

CORS policy does not include the frontend origin, or WebSocket upgrade is blocked by the reverse proxy.

**Diagnosis**

Browser console:
```
WebSocket connection to 'wss://hub.example.com/hubs/studies' failed:
  Error during WebSocket handshake: Unexpected response code: 403
```

Hub log:
```
[WRN] SignalR: CORS policy blocked connection from origin 'https://frontend.example.com'
```

**Resolution**

1. Add the frontend origin to `CorsOrigins` in Hub configuration:
```env
CorsOrigins__0=https://your-hub-domain.example.com
CorsOrigins__1=https://your-frontend-domain.example.com
```

2. Verify Nginx has WebSocket support configured for the `/hubs/` path:
```nginx
location /hubs/ {
    proxy_pass http://edgeguard_hub;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 86400s;
}
```

3. If WebSocket is blocked entirely by a proxy or firewall, SignalR will fall back to Server-Sent Events and then long polling — functionality is preserved but real-time latency increases. Resolve the WebSocket block for best performance.

---

## 12. Patient Merge Not Working

**Symptoms**
- ADT^A40 message received (NACK not returned)
- Patient records are not merged in EdgeGuard
- Duplicate patient records remain after merge message

**Cause**

Missing MRG segment in the ADT^A40 message, or the MRG-1 field (Prior Patient Identifier List) is empty.

**Diagnosis (Windows — primary)**

```powershell
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Tail 200 |
    Select-String -Pattern "merge|a40|mrg"
```

**Diagnosis (Linux, alternative)**

```bash
grep -i "merge\|a40\|mrg" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | tail -20
```

Expected warning:
```
[WRN] HL7 ADT^A40 patient merge: MRG segment missing or MRG-1 empty.
  MessageControlId=MSG042 — merge aborted.
```

**Resolution**

The ADT^A40 message MUST include the MRG segment with the prior patient identifier in MRG-1:

```
MSH|^~\&|HIS|SITE|EDGEGUARD|SITE|20250115120000||ADT^A40|MSG042|P|2.5
PID|1||NEW_MRN^^^SITE||...
MRG|OLD_MRN^^^SITE|
```

Verify with your HIS/RIS vendor that the MRG segment is included in all patient merge messages. See [hl7-integration-guide.md](../08-integrations/hl7-integration-guide.md) for the full ADT^A40 message specification.

---

## 13. Image Links Not Attached

**Symptoms**
- ORU^R01 messages are processed (ACK returned)
- Study shows in EdgeGuard but image viewer link is missing
- OBX segment is present in the message

**Cause**

OBX-2 (Value Type) is not set to a supported type for image references. EdgeGuard expects `RP` (Reference Pointer), `ED` (Encapsulated Data), or a URL-type value.

**Diagnosis**

```
[WRN] ORU^R01 OBX processing: OBX-2 value type 'TX' not recognized as image reference.
  MessageControlId=MSG077, OBX-3=18726-0 — image link skipped.
```

**Resolution**

Configure the sending system to use OBX value type `RP` or `ED` for image links:

```
OBX|1|RP|18726-0^Study URL^LN||https://pacs.example.com/viewer/study/1.2.3.4.5||||||F
```

Supported OBX-2 types for image references:

| Type | Description |
|------|-------------|
| `RP` | Reference Pointer — URL to image viewer |
| `ED` | Encapsulated Data — inline encoded data |
| `URL` | Universal Resource Locator (non-standard, supported for compatibility) |

---

## 14. MWL Query Returns Empty

**Symptoms**
- Modality queries MWL (Modality Worklist) and receives 0 results
- C-FIND-RSP shows `Status: 0000` (Success) but no matching datasets
- ORM^O01 messages were sent but no worklist entries exist

**Cause**

ORM^O01 messages not processed, study date filter mismatch, or scheduled procedure step status not set to `SCHEDULED`.

**Diagnosis (Windows — primary)**

```powershell
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Tail 300 |
    Select-String -Pattern "orm|worklist|scheduled"
```

**Diagnosis (Linux, alternative)**

```bash
grep -i "orm\|worklist\|scheduled" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | tail -30
```

**Resolution**

1. Verify ORM^O01 messages were received and processed — check the HL7 message log in Hub UI.

2. Check the scheduled procedure date: MWL queries from modalities often filter by today's date. Ensure ORM messages specify `StudyDate` matching today.

3. Verify the modality AE Title in the C-FIND request matches the node AE Title.

4. In Hub UI: **Studies → Worklist** — verify entries exist for today's date with status `Scheduled`.

5. Check the ORM^O01 includes the required OBR-4 (Universal Service Identifier) and OBR-36 (Scheduled Date/Time) fields.

---

## 15. High Memory on Hub

**Symptoms**
- Hub process memory grows continuously
- Performance degrades over time
- Eventual OOM kill or `OutOfMemoryException` in logs

**Cause**

Internal Channel queue for DICOM/HL7 message processing has exceeded its configured maximum, or there is a backlog of outbound PACS sends.

**Diagnosis (Windows — primary)**

```powershell
# Hub runs inside w3wp.exe under App Pool EdgeGuardHub
Get-WmiObject Win32_Process -Filter "Name='w3wp.exe'" |
    Where-Object { $_.CommandLine -match "EdgeGuardHub" } |
    Select-Object ProcessId,
        @{n="WorkingSetMB";e={[math]::Round($_.WorkingSetSize/1MB,1)}},
        @{n="PrivateMB";e={[math]::Round($_.PrivatePageCount/1MB,1)}}

# Check Hub logs for queue warnings
Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" -Tail 200 |
    Select-String -Pattern "queue|channel|backlog|MaxQueued"
```

**Diagnosis (Linux, alternative)**

```bash
ps aux | grep Dicom.Edge.Hub.Api
grep -i "queue\|channel\|backlog\|MaxQueued" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | tail -20
```

Expected warning:
```
[WRN] Channel queue is full (1000/1000 messages). New messages will be dropped.
  Consider increasing MaxQueuedMessages or improving consumer throughput.
```

**Resolution**

1. Increase the queue size in `appsettings.json`:
```json
{
  "Processing": {
    "MaxQueuedMessages": 5000
  }
}
```

2. Identify and resolve the root cause of the backlog — usually PACS send failures causing studies to retry repeatedly:

   **PowerShell (Windows):**
   ```powershell
   (Get-Content "C:\inetpub\EdgeGuard\Hub\logs\hub-$(Get-Date -Format yyyyMMdd).log" |
        Select-String "C-STORE SCU failed").Count
   ```

   **Bash (Linux):**
   ```bash
   grep "C-STORE SCU failed" /opt/edgeguard/hub/logs/hub-$(date +%Y%m%d).log | wc -l
   ```

3. If PACS is unreachable, resolve the connectivity issue (see [Study Stuck in Sending](#5-study-stuck-in-sending)) — the backlog will clear once sends succeed.

4. Monitor memory usage with Prometheus/Grafana alert when Hub memory exceeds 80% of the host's RAM.
