# Equipment Catalog & Per-Equipment MWL Filtering — Implementation Plan

> **Section:** 10-Remediation
> **Applies to:** EdgeGuard Hub (backend + UI), EdgeGuard Node (client/edge), Shared contracts
> **Status:** Approved plan — not yet implemented
> **Last updated:** 2026-06-05

---

## 1. Goal

Evolve the flat **allowed AE titles** whitelist into a dedicated, per-node **Equipment
Catalog**. The DICOMEdge (Node) becomes the authority that decides, **per connecting
equipment**, exactly which Modality Worklist (MWL) entries it is allowed to see — instead
of returning the whole worklist and trusting each modality to self-filter.

The catalog is authored in the **Hub**, managed through the **Hub UI**, and pushed to each
**Node**, reusing the proven `NodeDicomRoutingRule` synchronization pattern.

---

## 2. Approved design decisions

| Decision | Choice |
|---|---|
| Equipment → modality cardinality | **Multiple modalities per equipment** (M:N) |
| Modality representation | **Auto-seeded `Modality` reference catalog** with an `IsSupported` flag (image-level only: MG, US, DX, MR/RM, …) — not an enum |
| Equipment ↔ modality storage | **`EquipmentModality` join table** referencing the catalog |
| Behavior for an equipment **not** in the catalog | **Reject the DICOM association** (strict) |
| Edge entity | **New `Equipment` entity** (deprecate the latent `ModalityConfiguration`) |
| Rollout for this document | Plan only — no code changes yet |

---

## 3. Current state (analysis)

### 3.1 The flat allowed-AE-titles mechanism
- Setting key `dicom.allowed_ae_titles` — `src/shared/Dicom.Edge.Contracts/Configuration/SharedNodeSettingKeys.cs:51`.
- Materialized into `DicomServerOptions.AllowedCallingAeTitles` — `src/edge/Dicom.Edge.Node.DicomServer/DicomServerOptions.cs:25`.
- **Only** consumed for association-level whitelist in `CStoreScp.OnReceiveAssociationRequestAsync`
  — `src/edge/Dicom.Edge.Node.DicomServer/CStoreScp.cs:75-93`. No modality awareness.

### 3.2 The MWL gap
- `CStoreScp` calls `MwlHandler.QueryWorklistAsync(request.Dataset)` **without** the `CallingAE`
  — `src/edge/Dicom.Edge.Node.DicomServer/CStoreScp.cs:255`.
- `WorklistCFindHandler` filters only by the query keys the equipment sends
  (`ScheduledStationAETitle`, `Modality`) — `src/edge/Dicom.Edge.Node.DicomServer/WorklistCFindHandler.cs`.
- Result: **every equipment can see the whole worklist**; filtering is voluntary on the device side.

### 3.3 Latent (unused) entity
- `ModalityConfiguration` (`src/shared/Dicom.Edge.Models/Configuration/ModalityConfiguration.cs`)
  with table `modality_configurations` (`src/edge/Dicom.Edge.Node.Persistence/Context/EdgeNodeDbContext.cs:16`)
  exists but is **dead code**: no reader, no Hub management, no `ModalityType`, no MWL role.
  Per decision, it will be **deprecated** in favor of a new `Equipment` entity.

### 3.4 The reference pattern (to be mirrored)
`NodeDicomRoutingRule` is the proven node-scoped catalog: authored in the Hub, pushed to the Node.

```
Hub.Domain (aggregate + repo interface)
  → Hub.Persistence (EF config + migration + repo)
  → Hub.Application (service + push service + NodePushQueue / NodePushKind)
  → Hub.Api (controller)
  → [HTTP push] →
Edge.Node.Api (/sync controller, full-replace upsert)
  → Edge.Persistence (entity + hot-reloadable loader)
```

Reference files:
- `src/backend/Dicom.Edge.Hub.Domain/Aggregates/Routing/NodeDicomRoutingRule.cs`
- `src/backend/Dicom.Edge.Hub.Application/Routing/NodeDicomRoutingRuleService.cs`
- `src/backend/Dicom.Edge.Hub.Application/NodeConfiguration/NodePushRequest.cs` (`NodePushKind`)
- `src/backend/Dicom.Edge.Hub.Api/Controllers/NodeDicomRoutingRulesController.cs`
- `src/edge/Dicom.Edge.Node.Api/Controllers/DicomRoutingRulesController.cs` (`/sync` full-replace)

---

## 4. Target data model

### 4.1 `Equipment` (per node)

| Field | Type | Notes |
|---|---|---|
| `Id` | string (GUID) | PK |
| `NodeId` | string | Owning node |
| `AeTitle` | string(16) | Calling AE of the device — unique per node |
| `DisplayName` | string? | UI label |
| `Modalities` | M:N → `Modality` catalog | **Allowed modality set** via the `EquipmentModality` join — drives MWL filter |
| `StationAeTitle` | string? | Optional `ScheduledStationAETitle` (SPS) filter |
| `StationName` | string? | Optional `(0040,0010)` station name |
| `IpAddress` | string? | Optional AE+IP reinforcement on association |
| `IsEnabled` | bool | Disable without deleting |
| `Location`, `Department`, `Manufacturer`, `Model`, `Notes` | string? | Inventory metadata |
| `LastConnectionAt`, `IsOnline` | timestamps/bool | Observability |
| `CreatedAt/By`, `UpdatedAt/By` | audit | |

### 4.2 `Modality` (global, auto-seeded reference catalog)

Instead of an enum, modalities live in a **seeded reference table** so the set of valid
modality codes — and which ones are permitted — is data, not code.

| Field | Type | Notes |
|---|---|---|
| `Code` | string(2-4) | PK — DICOM Defined Term (e.g. `MG`, `US`, `DX`, `MR`, `CT`, `CR`) |
| `DisplayName` | string | Human label (e.g. "Mammography", "Ultrasound") |
| `IsSupported` | bool | **Only image-level modalities are permitted.** `true` for MG, US, DX, MR (RM), CT, CR, NM, PT, XA, RF, …; `false` for non-image objects (SR, KO, PR, AU, DOC, …) |
| `IsActive` | bool | Soft-disable a code without deleting |
| `SortOrder` | int | UI ordering |

- **Auto-seeded** on startup/migration from a canonical list (`ModalitySeed`) so a fresh
  install already has the full DICOM modality set with `IsSupported` pre-flagged.
- Seeding is **idempotent** (upsert by `Code`): re-running adds new codes / refreshes
  `DisplayName`/`IsSupported` without clobbering operator toggles of `IsActive`.
- **Authoring rule:** only `IsSupported = true && IsActive = true` modalities may be linked
  to an equipment or used for MWL filtering. This is the single source of truth for
  "MG, US, DX, RM, etc. son permitidos".
- The concrete proposed seed (codes + `IsSupported`) is in **Appendix A**.

> Note on `RM`: the DICOM Defined Term is **`MR`** (Magnetic Resonance). `RM` is the
> Spanish abbreviation; the catalog stores the DICOM code `MR` with display name that may
> read "Resonancia Magnética (RM)".

### 4.3 `EquipmentModality` (join table)

Many-to-many between `Equipment` and `Modality`.

| Field | Type | Notes |
|---|---|---|
| `EquipmentId` | string (FK) | → `Equipment.Id` (cascade delete) |
| `ModalityCode` | string (FK) | → `Modality.Code` |

Composite PK `(EquipmentId, ModalityCode)`. This is the set the Node uses to constrain MWL.

### 4.4 Edge behavior driven by the catalog
1. **Association (`CStoreScp`)** — reject the association when the `CallingAE` is absent
   from the (enabled) catalog. Optionally also require IP match when `IpAddress` is set.
   `dicom.allowed_ae_titles` is retained only as a transitional fallback when the catalog
   is empty.
2. **MWL C-FIND** — resolve equipment by `CallingAE`, then constrain the worklist to the
   intersection of:
   - the equipment's **allowed modality set**, and
   - its `StationAeTitle` (when configured),
   regardless of what the device put in (or omitted from) the query keys.

---

## 5. Workstreams

### Phase 1 — Shared contracts (`Dicom.Edge.Contracts`)
- `ModalityDto` (`Code`, `DisplayName`, `IsSupported`, `IsActive`, `SortOrder`).
- `EquipmentDto`, `CreateEquipmentRequest`, `UpdateEquipmentRequest` — the latter two carry
  a `ModalityCodes: string[]` (the selected catalog codes). Validation modeled on
  `src/shared/Dicom.Edge.Contracts/Configuration/ModalityConfigurationDto.cs`.
- `EquipmentSyncRequest` / `EquipmentSyncResponse` (mirror of `DicomRoutingRulesSyncRequest/Response`);
  each equipment in the payload carries its resolved `ModalityCodes`.
- Canonical `ModalitySeed` definition (code → display + `IsSupported`) placed in a shared
  location so Hub and Edge seed from the **same source**.

### Phase 2 — Hub (backend)
1. **Modality catalog**
   - **Domain** `Hub.Domain/Aggregates/Modalities/`: `Modality` reference entity + `IModalityRepository`.
   - **Persistence**: EF config, `DbSet`, **migration** (table `modalities`), and an
     **idempotent seeder** (upsert by `Code` from `ModalitySeed`) run at startup/migration.
   - **Api**: `ModalitiesController` (`GET /api/modalities`, with `?supportedOnly=true`) for the UI.
     Optional admin endpoint to toggle `IsActive`.
2. **Equipment (per node)**
   - **Domain** `Hub.Domain/Aggregates/Equipment/`: `NodeEquipment : AggregateRoot<string>`
     (factory `Create`, `Update`, `Enable/Disable`, `SetModalities(codes)`) + `INodeEquipmentRepository`.
     `SetModalities` **validates each code against the catalog** (`IsSupported && IsActive`) and
     rejects unsupported codes. Modeled on `NodeDicomRoutingRule`.
   - **Persistence**: EF `NodeEquipmentConfiguration` + `EquipmentModalityConfiguration` join,
     `DbSet`s, repository, **migration** (`node_equipment`, `equipment_modalities`).
3. **Application** `Hub.Application/Equipment/`: `NodeEquipmentService` (CRUD + `DispatchAsync`),
   `INodeEquipmentPushService` + HTTP impl. Add `NodePushKind.Equipment` to
   `NodePushRequest.cs` and route it in the `NodePushQueue` dispatcher.
4. **Api**: `NodeEquipmentController` (`/api/nodes/{nodeId}/equipment`, CRUD + enable/disable),
   mirroring `NodeDicomRoutingRulesController`. New permissions `EquipmentView` / `EquipmentManage`
   in `src/shared/Dicom.Edge.Security/Authorization/Permission.cs`.
5. **Backfill**: one-time migration converting each node's existing `dicom.allowed_ae_titles`
   into equipment entries (with an **empty** modality set, flagged for operator review) so
   strict rejection does not lock out already-working devices on upgrade.

### Phase 3 — Client / Edge Node
1. **Persistence**: new `Equipment` entity + `EquipmentModality` join + EF configs + migration;
   plus a local `Modality` reference table seeded from the **same shared `ModalitySeed`** as the
   Hub (idempotent upsert), so the node is self-contained for validation/display. Deprecate
   `ModalityConfiguration` (keep table temporarily, stop referencing it). Hot-reloadable
   `EquipmentLoaderService` with in-memory cache (equipment + its modality codes) + `LoadNowAsync`.
2. **Sync endpoint**: `EquipmentController` (`/api/equipment/sync`) — full-replace upsert of
   equipment **and** their `ModalityCodes`, calqued from `DicomRoutingRulesController.cs`, then
   `EquipmentLoaderService.LoadNowAsync`.
3. **Association validation**: in `CStoreScp.cs:75`, reject when `CallingAE` ∉ enabled catalog
   (with `RecordRejectionAsync` audit), optionally enforcing AE+IP.
4. **MWL filtering (core change)**:
   - Add `callingAe` parameter to `IWorklistCFindHandler.QueryWorklistAsync` and pass
     `Association.CallingAE` from `CStoreScp.cs:255`.
   - In `WorklistCFindHandler`, resolve the equipment, then drive
     `IWorklistManager.QueryAsync(from, to, modality)` (`src/edge/Dicom.Edge.Node.Worklist/IWorklistManager.cs:26`)
     by the equipment's modality set (union, applied per modality or extended to accept a set),
     intersecting with `StationAeTitle` when present. Unknown equipment never reaches MWL
     because the association is already rejected.

### Phase 4 — Hub UI (`dicomedge-ui`)
- New node-scoped resource under the `nodes` feature, following the
  `models/ infrastructure/ services/ presentation/` layout used by
  `src/frontend/dicomedge-ui/src/app/features/routing-rules`:
  - `equipment.models.ts`, HTTP service, `equipment-page` (list per node, filter by modality),
    `equipment-form-dialog` with a **multi-select modality picker sourced from
    `GET /api/modalities?supportedOnly=true`** (so only image-level codes — MG, US, DX, MR/RM,
    CT, CR, … — can be assigned), plus AE/IP fields and enable/disable.
  - A read-only modality catalog reference is also fine to surface (e.g. in settings) but is
    not required for the equipment flow.
  - Surface equipment as a tab in `node-detail-page` alongside config / PACS / routing rules.
  - Show push feedback ("configuration sent to node").

### Phase 5 — Testing & rollout
- **Unit**: modality seeder idempotency + `IsSupported` flags; `SetModalities` rejects
  unsupported codes; MWL filtering by modality set + station; association rejection for
  unknown AE; full-replace sync semantics.
- **Integration**: Hub→Edge push (equipment + modality codes); backfill from `allowed_ae_titles`.
- **Rollout**: ship the modality seed + backfill first so strict rejection is safe;
  `allowed_ae_titles` remains a fallback only while a node's catalog is empty. Document the
  cutover in `docs/04-features/`.

---

## 6. Documentation follow-ups (post-implementation)
- New `docs/04-features/equipment-catalog.md`.
- Update `docs/04-features/modality-worklist.md` (per-equipment filtering).
- Update `docs/05-api/hub-api-reference.md` and `docs/05-api/node-api-reference.md`.
- Note the `ModalityConfiguration` deprecation in `docs/01-overview/changelog.md`.

---

## 7. Open / deferred items
- Whether to keep `dicom.allowed_ae_titles` long-term or remove it once all nodes are migrated.
- Whether `IpAddress` enforcement is mandatory or advisory per equipment.
- Exact `IsSupported` membership of the seed list (Appendix A is the proposed default; confirm
  with clinical/ops — borderline rows are flagged there).
- Whether the `Modality` catalog should be a single global table (recommended) or per-node;
  global keeps codes consistent across the fleet while `IsActive` allows per-deployment trimming.

---

## 8. EF Core migrations required

Migrations are authored/run **by you**. Below are the phases that require one, the target
`DbContext`, the proposed migration name, and what it creates. Naming follows the existing
convention (`AddNodeDicomRoutingRules`, `AddNodePacsServers`, `ExpandWorklistItemFields`).

| Phase | DbContext | Proposed migration name | Creates / changes |
|---|---|---|---|
| 2 (Hub) | `HubDbContext` | `AddModalityCatalog` | Table `modalities` (`Code` PK, `DisplayName`, `IsSupported`, `IsActive`, `SortOrder`). Optional: seed via `HasData` from `ModalitySeed`. |
| 2 (Hub) | `HubDbContext` | `AddNodeEquipment` | Tables `node_equipment` + join `equipment_modalities` (FK → `node_equipment`, FK → `modalities`). |
| 2 (Hub) | `HubDbContext` | `BackfillEquipmentFromAllowedAeTitles` | **Data migration (optional)** — convert each node's `dicom.allowed_ae_titles` into `node_equipment` rows (empty modality set). Can also be a runtime seeder instead of a migration. |
| 3 (Edge) | `EdgeNodeDbContext` | `AddModalityCatalog` | Table `modalities` on the node (same shape as Hub; seeded from shared `ModalitySeed`). |
| 3 (Edge) | `EdgeNodeDbContext` | `AddEquipment` | Tables `equipment` + join `equipment_modalities`. |
| 3 (Edge) | `EdgeNodeDbContext` | `DropModalityConfiguration` | **Deferred/optional** — remove the deprecated `modality_configurations` table once nothing references it. |

Notes:
- Phases **1 (contracts)**, **4 (Hub UI)** and **5 (tests)** require **no** migrations.
- If you seed the catalog via `HasData`, the seed lives inside `AddModalityCatalog`; if you
  prefer the idempotent runtime seeder (recommended, easier to refresh), the migration only
  creates the empty table.
- The Hub and Edge both have a `modalities` table — same proposed name `AddModalityCatalog`
  in each context; they are independent migration histories.

---

## Appendix A — Proposed `Modality` seed list

Codes are DICOM Defined Terms (PS3.3 / PS3.16). The rule: **`IsSupported = true` only for
image-producing modalities**; non-image objects (reports, presentation states, waveforms,
registrations, most RT objects, etc.) are `IsSupported = false`. All rows seed with
`IsActive = true`; operators can trim per deployment via `IsActive`.

### A.1 Image modalities — `IsSupported = true`

| Code | DisplayName (ES) | Notes |
|---|---|---|
| `CR`  | Radiografía Computarizada | |
| `CT`  | Tomografía Computarizada | |
| `DX`  | Radiografía Digital | |
| `IO`  | Radiografía Intraoral | |
| `MG`  | Mamografía | |
| `MR`  | Resonancia Magnética | "RM" en español |
| `NM`  | Medicina Nuclear | |
| `PT`  | Tomografía por Emisión de Positrones (PET) | |
| `US`  | Ultrasonido | |
| `XA`  | Angiografía por Rayos X | |
| `RF`  | Radiofluoroscopía | |
| `RG`  | Radiografía Convencional | |
| `PX`  | Radiografía Panorámica | |
| `ES`  | Endoscopía | |
| `XC`  | Fotografía con Cámara Externa | |
| `OP`  | Fotografía Oftálmica | |
| `OPT` | Tomografía Oftálmica (OCT) | |
| `SM`  | Microscopía de Portaobjetos | |
| `GM`  | Microscopía General | |
| `IVUS`| Ultrasonido Intravascular | |
| `BDUS`| Densitometría Ósea por Ultrasonido | |
| `BMD` | Densitometría Ósea (Rayos X) | |
| `DG`  | Diafanografía | |
| `TG`  | Termografía | |
| `RTIMAGE` | Imagen de Radioterapia | Único objeto RT de tipo imagen |

### A.2 Non-image modalities — `IsSupported = false`

| Code | DisplayName (ES) | Motivo de exclusión |
|---|---|---|
| `SR`  | Documento de Reporte Estructurado | Reporte, no imagen |
| `KO`  | Selección de Objeto Clave | Documento de referencia |
| `PR`  | Estado de Presentación | Metadatos de visualización |
| `AU`  | Audio | Sonido, no imagen |
| `DOC` | Documento (PDF/CDA encapsulado) | Documento |
| `FID` | Marcadores Fiduciales | Marcadores espaciales |
| `REG` | Registro (Transformación Espacial) | Transformación espacial |
| `SEG` | Segmentación | Objeto derivado, no adquisición |
| `RWV` | Mapeo de Valores del Mundo Real | Mapeo derivado |
| `PLAN`| Plan | Objeto de planeación |
| `RTSTRUCT` | Conjunto de Estructuras de Radioterapia | Contornos, no imagen |
| `RTPLAN`   | Plan de Radioterapia | Plan de tratamiento |
| `RTDOSE`   | Dosis de Radioterapia | Malla de dosis |
| `RTRECORD` | Registro de Tratamiento de Radioterapia | Registro de tratamiento |
| `ECG` | Electrocardiografía | Forma de onda |
| `EPS` | Electrofisiología Cardíaca | Forma de onda |
| `HD`  | Forma de Onda Hemodinámica | Forma de onda |
| `RESP`| Forma de Onda Respiratoria | Forma de onda |
| `HC`  | Copia Impresa | Objeto de impresión |
| `OT`  | Otro | Catch-all ambiguo — deshabilitado por defecto |

### A.3 Borderline (confirm with clinical/ops before flipping)

| Code | DisplayName (ES) | Default | Consideración |
|---|---|---|---|
| `OAM` | Mediciones Axiales Oftálmicas | `false` | Produce mediciones, no imágenes pictóricas |
| `KER` | Queratometría | `false` | Dispositivo de medición |
| `SRF` | Refracción Subjetiva | `false` | Medición |
| `LEN` | Lensometría | `false` | Medición |
| `VA`  | Agudeza Visual | `false` | Medición |
