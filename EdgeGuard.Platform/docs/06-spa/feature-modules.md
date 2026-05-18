# SPA Feature Modules

Each feature is a self-contained directory under `src/app/features/`. All features are lazy-loaded; their code is only downloaded when the user navigates to the corresponding route. This document describes the purpose and key components of each feature.

---

## Nodes (`features/nodes`)

Manages EdgeGuard Node registration, configuration, and operational monitoring.

### Components

| Component | Path | Description |
|-----------|------|-------------|
| `NodesPage` | `presentation/nodes-page/` | Paginated list of all registered nodes with online/offline status indicators and quick-action buttons |
| `NodeDetailPage` | `presentation/node-detail-page/` | Multi-tab detail view: Overview, PACS Destinations, Telemetry, and DICOM Routing Rules |
| `NodeFormDialog` | `presentation/node-form-dialog/` | MatDialog for creating and editing node configuration (name, IP, port, AE title) |
| `NodeHealthCard` | `presentation/node-health-card/` | Card component showing live subsystem health (DICOM server, HL7 listener, database) polled from node `/health` |
| `NodeTelemetryPage` | `presentation/node-telemetry-page/` | Time-series charts of studies received per hour, active associations, and send queue depth |

### Infrastructure

- `NodesService` — CRUD operations, sync trigger, C-ECHO initiation.
- `NodeHealthService` — Polls node `/health` endpoint on a configurable interval.

### Key UX Flows

1. **Register Node** — User fills `NodeFormDialog`; on success, the `NodesPage` list refreshes and shows the new node as offline until the node service connects.
2. **Sync Config** — "Sync" button on `NodeDetailPage` calls `POST /api/nodes/{id}/sync`; a spinner is shown until the Hub returns.
3. **Test PACS** — C-ECHO button on the PACS tab of `NodeDetailPage` calls `POST /api/pacs-servers/{id}/test-echo/{nodeId}` and displays success/failure inline.

---

## Node DICOM Routing (`features/nodes/dicom-routing`)

Per-node DICOM routing rule management, nested within the Nodes feature.

| Component | Description |
|-----------|-------------|
| `NodePacsRoutingRulesDialog` | Lists all routing rules for a specific node with priority order, enable/disable toggles, and drag-to-reorder priority controls |
| `NodeDicomRuleFormDialog` | Create or edit a single routing rule: name, priority, condition list (modality, source AE title, institution, study description), and target PACS destination checkboxes |

Rules are evaluated in priority order (lower number = higher priority). A study is forwarded to all destinations whose conditions match.

---

## Studies (`features/studies`)

Central view for received DICOM studies.

| Component | Description |
|-----------|-------------|
| `StudiesPage` | Paginated, filterable table of all studies. Filters: free-text search, status, modality, source node, date range, urgency flag. Supports CSV export. |
| `StudyDetailPage` | Full study metadata, series list, routing history, and HL7 message associations. Links back to the patient record. |
| `StudyStatusBadge` | Inline badge component mapping `StudyStatus` enum values to color-coded labels: `Pending` (amber), `Sent` (green), `Failed` (red), `Cancelled` (gray) |

### Filtering

Filters are encoded in URL query parameters so that filtered views are bookmarkable and shareable. The filter signal is initialized from the current route's query params on component init.

---

## Patients (`features/patients`)

Patient demographic registry, populated from HL7 ADT messages and manual entry.

| Component | Description |
|-----------|-------------|
| `PatientsPage` | Paginated list with filters for name, MRN, creation node, and activity status. Supports CSV export and CSV bulk import. |
| `PatientDetailPage` | Demographics, contact information, study history, and merge history. Displays a `MergedPatientBadge` when the patient record has been merged into another. |
| `MergedPatientBadge` | Visual indicator (amber chip with an arrow icon) shown on records that have been merged; links to the surviving patient record. |

---

## HL7 (`features/hl7`)

Monitors inbound and outbound HL7 message processing.

| Component | Description |
|-----------|-------------|
| `Hl7MessagesPage` | Paginated queue of all HL7 messages with status, dispatch status, type, and timestamp columns. Filter by status, dispatch status, message type, and date range. |
| `Hl7MessageDetailPage` | Full raw HL7 message content (segments displayed in a monospace block), parsed fields summary, processing log, and reprocess action button. |

### Reprocess Action

The "Reprocess" button is shown only for messages with `status === 'Failed'`. Clicking it calls `POST /api/hl7/messages/{id}/reprocess` and refreshes the page status.

---

## HL7 Routing Rules (`features/routing-rules`)

Manages the rules that determine which HL7 messages are forwarded to external systems.

| Component | Description |
|-----------|-------------|
| `RoutingRulesPage` | Full CRUD list of HL7 routing rules with enable/disable toggles and priority indicators |
| `RoutingRuleFormDialog` | Create or edit a routing rule: name, conditions (message type, sending facility, patient class), and target destination |

This feature is distinct from the DICOM routing rules in the Nodes feature — it controls HL7 traffic, not DICOM C-STORE forwarding.

---

## Dashboard (`features/dashboard`)

Real-time operational overview.

| Component | Description |
|-----------|-------------|
| `DashboardPage` | Summary cards (studies today, active nodes, pending sends, failed sends) plus a live study feed |
| `StudyFeedComponent` | Scrolling list of the most recently received studies; updated in real time via SignalR `StudyReceived` events without page refresh |
| `StatCardComponent` | Reusable metric card with an icon, value, label, and optional trend indicator |

The SignalR connection is established when `DashboardPage` is initialized and torn down when the user navigates away (using the component's `DestroyRef`).

---

## Users (`features/users`)

User account management; accessible to Admin role only.

| Component | Description |
|-----------|-------------|
| `UsersPage` | Table of all application users with role badges and action buttons |
| `UserFormDialog` | Create user (with temporary password) or edit user details |
| `RoleSelectDialog` | Simple dialog for changing a user's role with a confirmation step |

---

## Settings (`features/settings`)

System-wide configuration panel; accessible to Admin role only.

| Component | Description |
|-----------|-------------|
| `SettingsPage` | Grouped key-value editor for all `SystemSettings` entries. Settings are organized into tabs: General, DICOM, HL7, Notifications. |

Each setting is edited inline; changes are saved individually via `PUT /api/system-settings/{key}`.

---

## Audit Logs (`features/audit`)

Immutable activity trail for compliance and troubleshooting.

| Component | Description |
|-----------|-------------|
| `AuditLogsPage` | Paginated, filterable log of all create, update, and delete operations. Filters: entity type, user, and date range. |
| `AuditLogDetailPanel` | Inline expansion panel showing before/after JSON diff for changed fields |

Audit logs are read-only; no create, edit, or delete operations are available in the UI.

---

## Queue Monitoring (`features/queue`)

Background job queue visibility and control.

| Component | Description |
|-----------|-------------|
| `QueueStatusPage` | Displays pending, processing, failed, and completed job counts. Auto-refreshes every 30 seconds. |
| `RetryFailedButton` | Button that calls `POST /api/queue/retry-failed` and confirms the number of jobs re-enqueued |

---

## WhatsApp (`features/whatsapp`)

Manages outbound WhatsApp notification configuration and delivery status. Patients with a registered phone number can receive study-ready notifications via the configured WhatsApp Business API provider.

| Component | Description |
|-----------|-------------|
| `WhatsappSettingsPage` | Configure API credentials, message templates, and trigger conditions |
| `WhatsappDeliveryPage` | Paginated log of all sent notifications with delivery status (delivered, read, failed) |

---

## PACS (`features/pacs`)

PACS server definitions shared across all nodes.

| Component | Description |
|-----------|-------------|
| `PacsServersPage` | List of all PACS server definitions with node-assignment counts |
| `PacsServerFormDialog` | Create or edit a PACS server (name, IP, port, AE titles) |
| `PacsNodeAssignmentDialog` | Manage which nodes have access to a specific PACS server |
