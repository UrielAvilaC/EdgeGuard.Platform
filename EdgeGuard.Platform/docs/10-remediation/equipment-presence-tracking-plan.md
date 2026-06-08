# Equipment Presence Tracking (Last-Seen / Online) — Implementation Plan (Variant A)

> **Section:** 10-Remediation
> **Applies to:** EdgeGuard Edge Node, EdgeGuard Hub (backend + UI), Shared contracts
> **Status:** Approved plan — not yet implemented
> **Last updated:** 2026-06-06

---

## 1. Goal

Give each piece of **equipment** (a modality device registered in the
[Equipment Catalog](../04-features/equipment-catalog.md)) a **passive presence indicator** —
"last seen" timestamp and a derived "online" flag — surfaced in the Hub UI.

Unlike PACS C-ECHO (an **active, outbound** check where the node is the SCU), equipment are
**inbound** SCUs: the node is the SCP. Many modalities do not run a Verification SCP, so an
active node→equipment C-ECHO would produce false "offline". Therefore presence is derived
**passively** from the associations the equipment actually opens against the node — no polling.

**Variant A:** capture activity on the Edge → report to the Hub via a **dedicated endpoint**
(`POST /api/edge/equipment-status`, mirroring `pacs-echo`) → **persist** `LastConnectionAt`
on `NodeEquipment` (reusing the existing, currently-unused `MarkConnected`) → compute
`IsOnline` at read time.

---

## 2. Current state (relevant facts)

- `NodeEquipment` (Hub domain) already has `LastConnectionAt`, `IsOnline` and a
  `MarkConnected(DateTime)` method — **all currently dead code** (nothing calls it).
- `NodeEquipmentDto` already exposes `lastConnectionAt` / `isOnline`; the Hub UI equipment
  page already receives them.
- `INodeEquipmentRepository.GetByNodeAndAeTitleAsync(nodeId, aeTitle)` already exists.
- Proven node→Hub reporting pattern to mirror:
  - Edge: `PacsCEchoHostedService` (periodic loop) → `IPacsEchoHubReporter` →
    `IHubSyncClient.ReportPacsEchoAsync` → `POST /api/edge/pacs-echo`.
  - Hub: `EdgeController` (`api/edge`) → `IEdgeNodeService.ProcessPacsEchoReportAsync`.
  - Contracts: `NodePacsEchoReportRequest` in `Dicom.Edge.Contracts/Hub/HubRequestDtos.cs`.
- Edge equipment catalog cache: `IEquipmentCatalog` / `InMemoryEquipmentCatalog` (singleton,
  atomic snapshot swap) — the model for the new activity tracker.
- Association entry point: `CStoreScp.OnReceiveAssociationRequestAsync`.

---

## 3. Design summary

```
Equipment associates → CStoreScp (accepted)         [capture, edge, event-driven]
     └─ IEquipmentActivityTracker.RecordSeen(callingAe)
EquipmentStatusReportHostedService (every ~60s)     [sync, edge → hub]
     └─ IHubSyncClient.ReportEquipmentStatusAsync(snapshot deltas)
          → POST /api/edge/equipment-status
EdgeController → EdgeNodeService                     [persist, hub]
     └─ NodeEquipment.MarkConnected(lastSeen)  (only when advanced)
GET /api/nodes/{id}/equipment                        [read, hub]
     └─ isOnline = lastConnectionAt >= now - OnlineWindow   (computed)
UI: equipment page                                   [display]
     └─ "En línea / Visto hace X" chip
```

Key decisions baked in:

| Concern | Decision |
|---|---|
| Channel | Dedicated `POST /api/edge/equipment-status` (Variant A) |
| Capture | On accepted association in `CStoreScp` (no polling) |
| Storage (Hub) | Persist `LastConnectionAt` via `MarkConnected`; update only when advanced |
| `IsOnline` | Computed at read time (`lastSeen + OnlineWindow`), never persisted |
| Report cadence | Periodic (default 60s), send only equipment seen since last report |
| `OnlineWindow` | Configurable, default 10 min |

---

## 4. Phases

### Phase 1 — Shared contracts (`Dicom.Edge.Contracts`)
Add to `Hub/HubRequestDtos.cs` (next to `NodePacsEchoReportRequest`):

```csharp
public sealed class NodeEquipmentStatusReportRequest
{
    public required string NodeId { get; init; }
    public DateTime ReportedAtUtc { get; init; } = DateTime.UtcNow;
    public required IReadOnlyList<EquipmentStatusEntry> Equipment { get; init; }
}

public sealed class EquipmentStatusEntry
{
    public required string AeTitle { get; init; }
    public DateTime LastSeenUtc { get; init; }
}
```

Response reuses the existing `EdgeOperationResult` shape (as the other `/api/edge/*` reports do).

### Phase 2 — Edge: activity capture
1. **`IEquipmentActivityTracker` + `InMemoryEquipmentActivityTracker`** (singleton, in
   `Dicom.Edge.Node.Persistence/Services` or alongside `InMemoryEquipmentCatalog`):
   - `void RecordSeen(string aeTitle)` — sets `lastSeen[aeTitle] = UtcNow` (thread-safe `ConcurrentDictionary`).
   - `IReadOnlyDictionary<string, DateTime> GetSnapshot()`.
   - `void MarkReported(IReadOnlyDictionary<string,DateTime> reported)` *(optional)* — to support delta reporting; or the reporter keeps the "last reported" map itself.
   - Place the interface in `Dicom.Edge.Abstractions/Equipment` (so `CStoreScp` can depend on it without referencing Persistence), register the impl as singleton in `PersistenceExtensions.RegisterInfrastructure`.
2. **Wire into `CStoreScp`**: add `IEquipmentActivityTracker` to `DicomScpDependencies` (passed
   from `DicomServerHostedService`, same as `IEquipmentCatalog`). In
   `OnReceiveAssociationRequestAsync`, after the association is accepted for a catalogued
   equipment, call `Deps.EquipmentActivityTracker.RecordSeen(callingAe)`.
   - Record on **accepted** associations only (rejections are already audited separately).

### Phase 3 — Edge: report to Hub
1. **`IHubSyncClient.ReportEquipmentStatusAsync(NodeEquipmentStatusReportRequest, ct)`** +
   implementation in `HubSyncClient` → `POST {hub}/api/edge/equipment-status` (copy
   `ReportPacsEchoAsync`, including auth headers and never-throw error handling).
2. **`EquipmentStatusReportHostedService`** (`BackgroundService`, in `Dicom.Edge.Node.Configuration`
   or `.Sender`, mirroring `PacsCEchoHostedService`'s loop):
   - Every `IntervalSeconds` (default 60), read `tracker.GetSnapshot()`, diff against the
     last-reported map, and report only entries whose `LastSeenUtc` advanced.
   - Skip when not registered (`hubClient.RegisteredNodeId` empty) — same guard as the PACS reporter.
   - Register as hosted service in the node composition root.
3. **Options**: `EquipmentPresenceOptions { bool Enabled = true; int IntervalSeconds = 60; }`
   bound from configuration (optional; can hardcode defaults initially).

### Phase 4 — Hub: receive + persist
1. **`EdgeController`**: add `[HttpPost("equipment-status")]` → `EdgeNodeService.ProcessEquipmentStatusReportAsync`.
   Mirror the auth/attributes of the existing `pacs-echo` action.
2. **`IEdgeNodeService` / `EdgeNodeService`**: `ProcessEquipmentStatusReportAsync(request, ct)`:
   - For each entry, `GetByNodeAndAeTitleAsync(nodeId, aeTitle)`; if found and
     `LastSeenUtc > equipment.LastConnectionAt`, call `equipment.MarkConnected(LastSeenUtc)`
     and `UpdateAsync`. Save once at the end (batch).
   - Ignore unknown AEs (cosmetic) and never throw on a single bad entry.
   - **Note:** `MarkConnected` currently also sets `IsOnline = true` and `UpdatedAt`. Adjust it
     to set only `LastConnectionAt` (+ `UpdatedAt`) so `IsOnline` is not persisted — see Phase 5.
3. **Write-amplification guard**: only the advanced-`lastSeen` filter on both edge (delta
   report) and hub (compare before update) — at most one row per equipment per interval, and
   only when it actually connected.

### Phase 5 — Hub: compute `IsOnline` at read time
1. Change `NodeEquipment.MarkConnected` to set `LastConnectionAt` (and `UpdatedAt`) only —
   stop persisting `IsOnline`.
2. Drop the persisted `IsOnline` from the read path: compute it in the API mapping
   (`NodeEquipmentMappingProfile.ToDto`) as
   `isOnline = LastConnectionAt is not null && LastConnectionAt >= DateTime.UtcNow - OnlineWindow`.
   - `OnlineWindow` as a constant/config (default `TimeSpan.FromMinutes(10)`).
   - Optionally keep the domain `IsOnline` property but mark it `[NotMapped]`/ignore in EF, or
     remove it from the entity and the column (a small migration). Simplest: keep the column
     but stop writing/reading it; remove later. (Avoid a migration in this plan.)

### Phase 6 — Hub UI
1. The `NodeEquipment` model already has `lastConnectionAt` / `isOnline`.
2. In `node-equipment-page.component.html`, add a presence chip per row:
   - `isOnline` → `<ui-chip color="success">En línea</ui-chip>`; else a muted
     "Visto {{ item.lastConnectionAt | relativeTime }}" (reuse `RelativeTimePipe`), or
     "Nunca conectado" when null.
3. Optional: a small legend / column header.

### Phase 7 — Tests & docs
- **Unit (edge):** `RecordSeen` updates snapshot; delta reporter sends only advanced entries;
  reporter no-op when unregistered.
- **Unit (hub):** `ProcessEquipmentStatusReportAsync` updates only advanced `LastConnectionAt`,
  ignores unknown AEs; `ToDto` online-window computation (boundary cases).
- **Docs:** add a "Presence tracking" section to
  [equipment-catalog.md](../04-features/equipment-catalog.md); add the endpoint to
  [node-api-reference.md](../05-api/node-api-reference.md) (Status/Telemetry) and to the
  changelog.

---

## 5. EF Core migrations

**None required.** `LastConnectionAt` / `IsOnline` columns already exist on `node_equipment`
(created by the `AddNodeEquipment` migration). This feature only changes which fields are
written/read. (A future optional `DropEquipmentIsOnlineColumn` migration could remove the
now-unused persisted `IsOnline`, but it is not needed for the feature to work.)

---

## 6. Files touched (summary)

| Layer | File(s) | Change |
|---|---|---|
| Contracts | `Hub/HubRequestDtos.cs` | `NodeEquipmentStatusReportRequest`, `EquipmentStatusEntry` |
| Edge abstractions | `Dicom.Edge.Abstractions/Equipment/IEquipmentActivityTracker.cs` | new interface |
| Edge persistence | `Services/InMemoryEquipmentActivityTracker.cs` + `PersistenceExtensions` | impl + DI singleton |
| Edge DICOM | `CStoreScp.cs`, `DicomServerHostedService.cs` (`DicomScpDependencies`) | capture on accept |
| Edge sync | `IHubSyncClient`, `HubSyncClient`, new `EquipmentStatusReportHostedService` | report loop |
| Hub API | `EdgeController.cs` | `POST /api/edge/equipment-status` |
| Hub app | `IEdgeNodeService`, `EdgeNodeService` | `ProcessEquipmentStatusReportAsync` |
| Hub domain | `NodeEquipment.MarkConnected` | set only `LastConnectionAt` |
| Hub API mapping | `NodeEquipmentMappingProfile.ToDto` | compute `isOnline` |
| Hub UI | `node-equipment-page.component.html` | presence chip |

---

## 7. Rollout & risk notes
- Fully backward compatible: no schema change, no breaking contract change. If the node is old
  (no reporter), equipment simply show "Nunca conectado".
- Volume: presence reports are tiny and rate-limited to one interval; deltas keep Hub writes
  proportional to *active* equipment only.
- `OnlineWindow` should be ≥ the report interval (default 10 min ≫ 60 s) so a single missed
  report does not flap the indicator.
- Semantics caveat: "online" means *recently associated*, not *currently reachable*. Document
  this so operators don't read it as an active liveness probe.
