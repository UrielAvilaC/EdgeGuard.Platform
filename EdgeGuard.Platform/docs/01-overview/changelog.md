# Changelog

All notable changes to EdgeGuard Platform are documented here. This file follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) conventions. Versions are tagged on the `main` branch; each release is accompanied by a GitHub Release with migration notes where applicable.

---

## Unreleased

### feat
- **Equipment catalog & per-equipment MWL filtering** — Operators register, per Edge Node, which modality devices may connect (by calling AE title) and which modalities each one is allowed to see. The Edge Node now **rejects associations** from uncatalogued/disabled equipment and **filters the Modality Worklist per device** (by its allowed modality set ∩ scheduled station AE) instead of returning the whole worklist. Equipment is authored in the Hub (Node → Equipos), validated against a seeded modality reference catalog, and pushed to nodes via `POST /api/equipment/sync`. See [Equipment Catalog](../04-features/equipment-catalog.md).
- **Modality reference catalog** — New global, auto-seeded `modalities` table (Hub + Edge) flagging image-level modalities as supported; only supported & active modalities may be assigned to equipment. Seeded idempotently from a shared source on startup.
- **Equipment AE+IP enforcement & connection auditing** — When an equipment declares an IP address, the Edge Node enforces AE+IP on association (source host must match). Every equipment association decision — accepted or rejected (not registered / disabled / IP mismatch) — is persisted to the node's `dicom_associations` audit table.
- **Equipment presence tracking (last-seen / online)** — The Edge Node passively records each equipment's most recent association and reports deltas to the Hub (`POST /api/edge/equipment-status`); the Hub persists `LastConnectionAt` and derives `IsOnline` at read time from a configurable window (`EquipmentPresence:OnlineWindowMinutes`, default 10). Surfaced on the node Equipment page ("En línea / Visto hace X / Nunca conectado"). No active polling — avoids false offline for SCU-only modalities.
- **Unified durable outbox** — Node-sync pushes (config, rules, PACS, equipment) now flow through a durable `node_outbox_messages` store with at-least-once delivery, exponential backoff, dead-lettering and per-node lanes — replacing the in-memory `NodePushQueue` (lost on restart). It shares a single dispatcher skeleton (`OutboxDispatcherBase`) and an `outbox_topics` catalog (FK) with the existing notification outbox. A unified read view (`vw_outbox_activity`) powers a new **Outbox** monitoring page (REST `GET /api/outbox`, retry/dead-letter actions, live SignalR `OutboxEntryChanged`). See [Outbox unification plan](../10-remediation/unified-outbox-retention-masterplan.md).
- **DB-driven retention, applied hot** — All data-retention policies live in `system_settings` (no appsettings); retention now reads `IOptionsMonitor` each cycle, so changing a retention setting applies without a restart. Adds per-outbox retention (`retention.notification_days`, `retention.node_outbox_days`).
- **Live HL7 listener reconfiguration** — Changing `hl7.tcp_port` / `hl7.tcp_enabled` via the settings API now **reloads config and rebinds the listener on demand** (explicit restart, no host restart), with port validation and a confirmation response (`{ applied, port, running }`). Backed by a re-entrant `Hl7TcpListener` + `IRuntimeConfigReloader`.

### fixed
- **System settings were never persisted** — `SystemSettingsService.SetAsync` (and `SeedDefaultsAsync`) updated the tracked entity but never committed (no `SaveChanges`/UnitOfWork), so changing any setting via the API/UI silently had no effect. Now commits via `IUnitOfWork`. *(Found during outbox/hot-reload end-to-end verification.)*

### renamed
- **WhatsApp → Notification (channel-agnostic parts)** — `WhatsAppAutoSendRule` → `NotificationAutoSendRule` (table `whatsapp_auto_send_rules` → `notification_auto_send_rules`); retention key `retention.whatsapp_notification_days` → `retention.notification_days`. Channel-specific WhatsApp artifacts (templates, sender, Twilio provider config) keep their names.

### deprecated
- **`ModalityConfiguration` (Edge) / `modality_configurations` table** — Superseded by the new `Equipment` entity and equipment catalog. The type is no longer referenced by application logic; the table is retained for now and will be dropped in a future migration (`DropModalityConfiguration`).
- **`dicom.allowed_ae_titles`** — Retained only as a fallback used while a node's equipment catalog is empty (rollout safety). Once equipment is registered, the catalog is authoritative. Long-term removal is under review.

### migrations
- Hub (`HubDbContext`): `AddModalityCatalog`, `AddNodeEquipment`.
- Hub (`HubDbContext`): `RenameAutoSendRuleToNotification` (table + retention setting key), `AddOutboxTopics`, `AddNotificationTopicId` (column + FK + backfill), `AddNodeOutboxMessages`, `AddOutboxActivityView` (raw-SQL view). Topic catalog + new `system_settings` keys are runtime-seeded (no migration).
- Edge (`EdgeNodeDbContext`): `AddModalityCatalog`, `AddEquipment` (auto-applied on node startup).

---

## v1.0.0 — 2026-05-16

### feat
- **DICOM routing rules engine** — Priority-ordered rules match on Modality, AE Title, Body Part Examined, and Study Description (regex). Multiple PACS destinations per rule. Rules pushed to nodes in real time.
- **HL7 MRG segment processing** — `ADT^A40` patient-merge events are now fully handled: prior patient records are merged into the surviving record, duplicate studies are re-linked, and an audit entry is created.
- **HL7 OBX segment storage** — Observation/result segments from `ORU^R01` messages are stored alongside the study record and surfaced in the SPA study detail view.
- **Angular SPA — complete feature set** — All planned SPA screens are implemented: patient list, study browser, routing rule editor, PACS destination manager, node health dashboard, user management, and audit log viewer.
- **Audit log** — Every state-changing operation in the Hub (study received, routing rule changed, user role updated, patient merged) generates an immutable audit record in PostgreSQL.
- **Bootstrap token UI flow** — First-login wizard guides administrators through bootstrap token entry, admin account creation, and initial system configuration.

### fix
- Resolved race condition where concurrent C-STORE associations from the same modality could produce duplicate `StudyInstanceUID` records in SQLite.
- HL7 ACK response now correctly includes the original `MSH-10` (Message Control ID) in `MSA-3`.
- JWT refresh token rotation now correctly invalidates the previous token family on reuse detection.
- Soft-deleted patients no longer appear in MWL C-FIND responses.

### breaking
- `RoutingRule.Priority` field renamed from `Order` to `Priority` in the REST API and database schema. Run `dotnet ef database update` to apply the migration.
- `EDGEGUARD_HUB_CONNECTIONSTRING` environment variable is now the canonical configuration source. The `ConnectionStrings:HubDatabase` appsettings key is retained as a fallback but is deprecated.

---

## v0.9.0 — 2026-02-28

### feat
- **Multi-PACS support** — A single routing rule can now target multiple PACS destinations. The Edge Node forwards studies to each destination in parallel and reports per-destination delivery status.
- **SignalR real-time notifications** — The Hub broadcasts study-received, study-delivered, and node-health-changed events to connected SPA clients via a persistent WebSocket connection.
- **WhatsApp alert integration** — Configurable alerts (study received, delivery failure, node offline) can be sent to one or more WhatsApp numbers via a configurable messaging gateway. Configured under `Notifications:WhatsApp` in appsettings.
- **Node health dashboard** — SPA displays per-node status: last heartbeat, queue depth, active associations, and SQLite database size.
- **PACS C-ECHO verification** — Hub UI can trigger an on-demand C-ECHO to any configured PACS destination via the Edge Node and display the result.

### fix
- Edge Node no longer retains sent instances in SQLite indefinitely; a configurable retention policy (`DicomServer:InstanceRetentionDays`) deletes delivered instances after N days.
- SignalR connection drop during Hub restart no longer causes the SPA to display stale data; reconnection logic with exponential back-off was added.
- Multi-part DICOM series with more than 500 instances no longer exceed the default ASP.NET Core request body size limit.

### breaking
- `Node.Status` enum values changed: `Online` → `Healthy`, `Offline` → `Unreachable`. Update any monitoring scripts that parse the `/api/nodes` response.

---

## v0.8.0 — 2025-11-15

### feat
- **HL7 MLLP TCP listener** — Dedicated `Hl7ListenerService` hosted service opens a TCP socket on port 8001 (configurable). Handles concurrent connections with one thread per connection.
- **ADT pipeline** — `ADT^A01` creates or updates a patient record in the Hub. `ADT^A40` triggers the patient-merge workflow (MRG processing completed in v1.0.0).
- **ORM pipeline** — `ORM^O01` creates a worklist entry and pushes it to the appropriate Edge Node's MWL via the Hub→Node push mechanism.
- **ORU pipeline** — `ORU^R01` attaches diagnostic report metadata to the associated study record.
- **HL7 ACK / NACK generation** — Every inbound MLLP message receives an HL7 v2.x `ACK` response (`AA` on success, `AE` on application error, `AR` on rejection).
- **PHI redaction in HL7 logs** — Patient name, MRN, and date of birth are masked before HL7 segment content is written to any log sink.

### fix
- MLLP framing correctly handles messages split across multiple TCP receive buffers.
- ORM messages with a missing `OBR` segment no longer crash the listener; they are rejected with an `AE` ACK and a structured log entry.

---

## v0.7.0 — 2025-08-20

### feat
- **JWT authentication** — All Hub API endpoints (except `/api/auth/login` and `/api/health`) require a valid Bearer token. Tokens are signed with a configurable HMAC-SHA256 secret.
- **Refresh token rotation** — Refresh tokens are single-use. Each refresh issues a new token pair. Reuse of a consumed refresh token triggers family revocation (all tokens for the user are invalidated).
- **Role-based authorization** — Three built-in roles: `Admin`, `Operator`, `Viewer`. Role assignments are stored in PostgreSQL and checked at the controller action level with `[Authorize(Roles = "...")]`.
- **Bootstrap token** — On first startup with an empty `Users` table, the Hub logs a one-time bootstrap token that grants access to the user-creation endpoint.
- **Permission middleware** — Fine-grained permission claims (`study:read`, `routing:write`, `node:manage`, etc.) are embedded in the JWT and evaluated by a custom authorization handler.

### fix
- Token expiry is now correctly enforced even when the server clock drifts; tokens include `nbf` (not-before) and `exp` claims validated against UTC.

---

## v0.6.0 — 2025-05-10

### feat
- **Angular 19 SPA — standalone components** — Full migration from NgModule-based architecture to Angular standalone components and `provideRouter`-based lazy routing.
- **Clinical dashboard** — Summary tiles: studies received today, pending deliveries, active nodes, unacknowledged alerts. Line chart of daily study volume (last 30 days).
- **OpenTelemetry integration** — Hub exports OTLP traces (HTTP exporter) and Prometheus-compatible metrics when `OpenTelemetry:Enabled = true`. Span names follow the OpenTelemetry semantic conventions for ASP.NET Core and Entity Framework Core.
- **Seq structured logging** — Optional Serilog Seq sink activated by setting `Seq:ServerUrl`. All Hub and Node log events include `CorrelationId`, `NodeId` (on Node), and `MessageType` (on HL7 events) as structured properties.
- **Rate limiting** — Two named policies registered with ASP.NET Core's built-in rate limiter: `edge` (100 req/min, used for node-to-Hub calls) and `api` (200 req/min, used for SPA and external clients).

### fix
- Angular build no longer includes source maps in the production bundle copied to `wwwroot`.
- PostgreSQL connection pool is now bounded (`MaxPoolSize=20`) to prevent exhaustion under load.

---

## v0.5.0 — 2025-02-01

### feat
- **Clean Architecture scaffolding** — Hub API organized into `Domain`, `Application`, `Infrastructure`, and `Api` projects. Dependency inversion enforced via interfaces. Writes flow through application services; domain events are dispatched after `SaveChanges` by an EF Core interceptor.
- **PostgreSQL persistence** — EF Core 10 with code-first migrations. Hub DB schema includes: `Patients`, `Studies`, `Instances`, `Nodes`, `PacsDestinations`, `RoutingRules`, `Users`, `RefreshTokens`, `AuditLogs`, `Hl7Messages`.
- **First Edge Node** — .NET Worker Service with fo-dicom C-STORE SCP. Receives DICOM studies and stores instances in SQLite. Posts a study-received notification to the Hub over HTTP.
- **Node registration** — Edge Node posts `POST /api/nodes/register` with `NodeId`, `AeTitle`, `IpAddress`, and `ApiPort` on first boot. Hub records the node and returns a node-scoped JWT.
- **Serilog PHI redaction** — Custom `PhiRedactionEnricher` replaces PII field values with `[REDACTED]` before log events reach any sink. `Strict` mode (Node default) redacts all patient fields; `Relaxed` mode (Hub default) retains accession number and study UID.
- **Soft delete pattern** — `ISoftDeletable` interface implemented on all major aggregates. `DeletedAt` timestamp set on logical deletion; EF Core global query filter excludes soft-deleted records from all queries.
- **EF Core global query filters** — `DeletedAt IS NULL` filter applied globally. Queries requiring access to deleted records call `.IgnoreQueryFilters()` explicitly (audit and merge operations only).

### breaking
- This is the initial public release. No prior version exists.
