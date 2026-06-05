# Plan de Implementación — Unificación del AE Title del Nodo

> **Estado:** Borrador — pendiente de aprobación
> **Fecha:** 2026-05-31
> **Objetivo:** Establecer **una sola fuente de verdad** para el AE Title del nodo: `DicomServer:AeTitle`. Todas las demás menciones se **derivan** de ella. Eliminar el drift de defaults/seeds.
> **Esfuerzo:** ~1 día-persona · **Riesgo:** Medio (cambia el Calling AE saliente hacia PACS)

---

## Contexto: el AE del nodo está hoy en 4 fuentes independientes

| Fuente (config path) | Clave DB (`node_settings`) | Rol | Default/seed actual |
|---|---|---|---|
| **`DicomServer:AeTitle`** ← *canónica* | `dicom.ae_title` | SCP **Called AE** (entrada de imágenes/MWL/C-ECHO) | `EDGE_NODE` |
| `HubConnection:AeTitle` | `node.ae_title` (General) | AE de registro en el Hub | `EDGE_NODE` (appsettings) / `""` (seed) |
| `PacsSender:LocalAeTitle` | `sender.local_ae_title` | SCU **Calling AE** (salida hacia PACS) | **`EDGENODE`** ⚠️ (drift) |
| `Node.AeTitle` (Hub, persistido) | — | Copia almacenada del AE de registro | = lo que llega en el registro |

**Cableado DB→config** (`NodeDatabaseConfigurationProvider`):
- `dicom.ae_title` → `DicomServer:AeTitle` (línea 138)
- `node.ae_title` → `HubConnection:AeTitle` (línea 122)
- `sender.local_ae_title` → `PacsSender:LocalAeTitle` (línea 190)

**Drift confirmado:** el SCU sale como `EDGENODE` mientras todo lo demás es `EDGE_NODE` → un PACS que filtra por Calling AE puede rechazar al nodo, y el AE que el Hub almacena no coincide con el que ve el PACS.

---

## Estrategia

`DicomServer:AeTitle` (DB: `dicom.ae_title`) es **la única clave editable**. `HubConnection:AeTitle` y `PacsSender:LocalAeTitle` dejan de ser configurables y pasan a **derivarse** de la canónica en dos chokepoints:

1. **`PostConfigure`** (cubre appsettings + cualquier fuente, y re-deriva en cada reload de `IOptionsMonitor` → respeta pushes del Hub).
2. **`NodeDatabaseConfigurationProvider`** (cubre la config DB/pusheada): deriva los paths redundantes de `dicom.ae_title` en vez de mapear `node.ae_title`/`sender.local_ae_title`.

Constante única de fallback: `const string DefaultNodeAeTitle = "EDGE_NODE"`.

---

## FASE 1 — Derivación (forzar fuente única)

### 1.1 Constante compartida del path canónico
- **Nuevo:** en `Dicom.Edge.Contracts/Configuration/` (o `Dicom.Edge.Common`):
  ```csharp
  public static class NodeAeTitle
  {
      public const string ConfigPath = "DicomServer:AeTitle";
      public const string Default    = "EDGE_NODE";
  }
  ```

### 1.2 Derivar el SCU (`PacsSender:LocalAeTitle`)
- **`src/edge/Dicom.Edge.Node.Sender/SenderExtensions.cs`** — tras `Configure<PacsSenderOptions>`:
  ```csharp
  services.PostConfigure<PacsSenderOptions>(o =>
      o.LocalAeTitle = configuration[NodeAeTitle.ConfigPath] ?? NodeAeTitle.Default);
  ```

### 1.3 Derivar el AE de registro (`HubConnection:AeTitle`)
- **`src/edge/Dicom.Edge.Node.Configuration/ConfigurationExtensions.cs`** — ampliar el `PostConfigure<HubConnectionOptions>` ya existente (línea 16):
  ```csharp
  services.PostConfigure<HubConnectionOptions>(opts =>
  {
      if (opts.ApiPort == 0)
          opts.ApiPort = configuration.GetValue("NodeApi:Port", 5120);
      opts.AeTitle = configuration[NodeAeTitle.ConfigPath] ?? NodeAeTitle.Default; // ← unificado
  });
  ```

### 1.4 Derivar en la config DB/pusheada
- **`NodeDatabaseConfigurationProvider.cs`**:
  - Quitar el mapeo `General.AeTitle → HubAeTitle` (línea 122) y `PacsSender.LocalAeTitle → PacsSenderLocalAeTitle` (línea 190) **para el AE**.
  - Tras `MapDicomServer`, derivar:
    ```csharp
    if (cfg.TryGetValue(ConfigPaths.DicomAeTitle, out var ae) && !string.IsNullOrWhiteSpace(ae))
    {
        cfg[ConfigPaths.HubAeTitle]             = ae;
        cfg[ConfigPaths.PacsSenderLocalAeTitle] = ae;
    }
    ```

**Resultado Fase 1:** el SCU, el registro y el SCP siempre presentan el mismo AE (= `DicomServer:AeTitle`), sin importar valores viejos en DB/appsettings.

---

## FASE 2 — Eliminar el drift en defaults/seeds

| Archivo | Línea | Cambio |
|---|---|---|
| `src/edge/Dicom.Edge.Node.Sender/PacsSenderOptions.cs` | 11 | `"EDGENODE"` → `"EDGE_NODE"` (o `""`, ya que se deriva) |
| `src/edge/Dicom.Edge.Node.Persistence/Seed/NodeSettingsSeed.cs` | 125 | `"EDGENODE"` → `"EDGE_NODE"` |
| `src/edge/Dicom.Edge.Node.Persistence/Seed/NodeSettingsSeed.cs` | 55 | `General.AeTitle` `""` → `"EDGE_NODE"` |
| `src/shared/Dicom.Edge.Contracts/Configuration/SharedNodeSettingDefaults.cs` | 90 | `"EDGENODE"` → `"EDGE_NODE"` |
| `src/edge/Dicom.Edge.Node.Persistence/Services/NodeSettingsService.cs` | 314 | fallback ya es `"EDGE_NODE"` (verificar consistencia) |

---

## FASE 3 — Limpieza de appsettings y settings redundantes

### 3.1 appsettings (quitar la clave duplicada)
- **`appsettings.json` / `.Development.json` / `.Production.json`** del Node — eliminar `HubConnection:AeTitle` (queda solo `DicomServer:AeTitle`). `PacsSender:LocalAeTitle` ya no está en appsettings.

### 3.2 Sync del Hub (clave General redundante)
- **`HubConfigSyncHostedService.cs`** línea 135 — `SyncIfEmptyAsync(..., General.AeTitle, Opts.AeTitle, ...)`: tras la unificación `Opts.AeTitle` ya es la canónica; mantener o eliminar el sync de `node.ae_title` (recomendado: dejar de exponer `node.ae_title` como AE).

### 3.3 UI de settings (SPA)
- Marcar `sender.local_ae_title` y `node.ae_title` como **derivados/solo-lectura** ("heredado de DICOM AE Title"), o esconderlos. La única edición de AE es `dicom.ae_title`.

---

## FASE 4 — Hub y transversales

- **`EdgeNodeService.cs`** líneas 39-40 — corregir el comentario obsoleto/falso (*"AeTitle is no longer stored on the Hub side…"*): el Hub **sí** persiste `Node.AeTitle` (línea 66, `OwnsOne`).
- **Validación al arranque (Node):** validar la canónica contra `GuardClause.AgainstInvalidAeTitle` (`^[A-Z0-9_]{1,16}$`) y loguear el AE efectivo. Durante la transición, si `node.ae_title`/`sender.local_ae_title` en DB difieren de `dicom.ae_title`, emitir **warning** (visibilidad del drift heredado).
- **Docs:** actualizar `docs/02-getting-started/environment-variables.md` y `docs/08-integrations/dicom-conformance.md` para documentar que el AE del nodo es **único** (`DicomServer:AeTitle`).

---

## Tests

- **Unit (provider):** dado `dicom.ae_title="SITE_A"` + `sender.local_ae_title="OLD"` en DB → `PacsSender:LocalAeTitle` y `HubConnection:AeTitle` resuelven a `SITE_A`.
- **Unit (PostConfigure):** sin DB, solo appsettings con `DicomServer:AeTitle="SITE_B"` → `LocalAeTitle == HubConnection.AeTitle == "SITE_B"`.
- **Unit:** AE inválido (`"site a"`, >16 chars) → falla la validación de arranque.
- **Integración:** registrar el nodo → el Hub almacena `Node.AeTitle == DicomServer:AeTitle`.
- **Integración:** C-STORE saliente → el PACS ve Calling AE = `DicomServer:AeTitle` (no `EDGENODE`).
- **Reload:** push del Hub cambia `dicom.ae_title` → SCU y registro re-derivan sin reiniciar.

*(Pre-requisito: proyecto de tests del Node, hoy inexistente.)*

---

## Rollout y rollback

- **Cambio de comportamiento:** el Calling AE saliente hacia PACS pasa de `EDGENODE` a `DicomServer:AeTitle` (`EDGE_NODE` por defecto). **Riesgo MEDIO** si algún PACS hace whitelist por Calling AE.
- **Mitigación:** antes del deploy, auditar PACS que filtren por AE del nodo; alinear su whitelist con `DicomServer:AeTitle`.
- **Rollback:** revert de la Fase 1 (vuelven las 3 fuentes independientes). Sin migración destructiva; los valores DB viejos siguen presentes pero ignorados tras la unificación.
- **Migración opcional one-shot:** poner `node.ae_title` y `sender.local_ae_title` = `dicom.ae_title` en la DB de cada nodo para dejar consistente el almacén (no requerido por el código tras la derivación).

---

## Orden de ejecución (checklist)

- [ ] **T1** Constante `NodeAeTitle` (path + default).
- [ ] **T2** `PostConfigure<PacsSenderOptions>` deriva `LocalAeTitle`.
- [ ] **T3** `PostConfigure<HubConnectionOptions>` deriva `AeTitle`.
- [ ] **T4** `NodeDatabaseConfigurationProvider`: derivar HubAeTitle + PacsSenderLocalAeTitle de `dicom.ae_title`.
- [ ] **T5** Fix defaults/seeds `EDGENODE`→`EDGE_NODE` (Fase 2).
- [ ] **T6** Quitar `HubConnection:AeTitle` de appsettings.
- [ ] **T7** UI: marcar `sender.local_ae_title` / `node.ae_title` como derivados.
- [ ] **T8** Validación + log de arranque del AE canónico.
- [ ] **T9** Fix comentario en `EdgeNodeService` (Hub).
- [ ] **T10** Docs.
- [ ] **T11** Tests.

---

## Aprobaciones

| Rol | Responsabilidad | Aprobado |
|---|---|---|
| Tech Lead | Diseño de derivación (chokepoints) | ☐ |
| DevOps | Auditoría de whitelists de AE en PACS | ☐ |
| QA Lead | Tests de derivación y reload | ☐ |
