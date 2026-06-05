# Plan de Remediación P1 — EdgeGuard Platform (Items 1-5)

> **Estado:** Borrador inicial — pendiente de aprobación
> **Fecha:** 2026-05-31
> **Alcance:** Items P1-1 a P1-5 del backlog post-P0
> **Objetivo:** Eliminar bloqueos del SPA, cerrar vectores de DoS, garantizar correctitud bajo concurrencia y proteger la integridad del ciclo de vida del estudio

---

## Resumen Ejecutivo

| # | Hallazgo | Severidad | Categoría | Esfuerzo | Fase | Estado |
|---|---|---|---|---|---|---|
| P1-1 | Push síncrono cuelga el SPA | 🟠 High | UX / Estabilidad | M | 1 | Pendiente |
| P1-2 | `MaxClients` no plumbed a fo-dicom (DoS) | 🟠 High | Seguridad / DoS | XS | 1 | Pendiente |
| P1-3 | Race conditions en Study/Patient/Series | 🟠 High | Integridad de datos | M | 2 | Pendiente |
| P1-4 | Sin retry / MaxRetries en Sender | 🟠 High | Entrega de datos | S | — | ✅ **Implementado** |
| P1-5 | State machine del Study sin validar transiciones | 🟠 High | Correctitud de dominio | M | 2 | Pendiente |

**Esfuerzo restante estimado:** ~5 días-persona
**Leyenda esfuerzo:** XS=2-4h · S=½ día · M=1-2 días · L=2-4 días

### Orden por fases

- **Fase 1 (Estabilidad / DoS)** — P1-1 y P1-2. Sin cambios de dominio, bajo riesgo de regresión, alto retorno inmediato (UX + cierre de DoS).
- **Fase 2 (Correctitud)** — P1-3 y P1-5. Tocan dominio y persistencia; mayor riesgo, requieren tests de concurrencia y de máquina de estados.

---

## P1-4 · Retry en el Sender — ✅ YA IMPLEMENTADO

Incluido aquí para trazabilidad. Resuelto en `src/edge/Dicom.Edge.Node.Sender/FoDicomPacsSender.cs`:

- El envío ahora reintenta hasta `PacsSenderOptions.MaxRetries` con backoff exponencial (2s→4s…, cap 30s).
- Cada reintento reenvía **solo** las instancias no confirmadas (C-STORE es idempotente por SOP Instance UID).
- Un envío parcialmente fallido ya **no** se reporta como `Ok` — retorna `Fail` con el detalle `{fallidas}/{total}` (corrige la pérdida silenciosa de instancias).

Pendiente opcional: cubrir con tests unitarios cuando exista el proyecto de tests (ver sección final).

---

## FASE 1 — Estabilidad y DoS

### P1-1 · Sacar el push de configuración del request path

**Problema:** Varios endpoints empujan configuración a los nodos **dentro del request HTTP del SPA, de forma `await`-bloqueante**:

- `NodeDicomRoutingRuleService` — `await pushService.PushAsync(rule.NodeId, ct)` en cada CRUD (líneas 42, 70, 82, 94, 106, 121).
- `NodeConfigurationController.PushConfig` — `await _pushService.PushConfigAsync(nodeId, ct)` (línea 111).
- `NodeService` — `await push.PushConfigAsync(...)` / `push.PushAsync(...)` (líneas 153, 174).

Cada `PushAsync` es una llamada HTTP al nodo con timeout. El usuario del SPA queda bloqueado hasta el round-trip (o el timeout) del nodo. Si el nodo está caído, el SPA "se cuelga" hasta el timeout completo.

El fan-out a *todos* los nodos en `PacsServerService.PushPacsToAllNodesAsync` **ya** es fire-and-forget (`_ = PushPacsToAllNodesAsync()`, scope propio, `CancellationToken.None`), pero tiene dos defectos: es **secuencial** (`foreach … await`) y es **unobserved** (`_ =` descarta la Task → fallos invisibles, sin feedback al SPA).

**Solución:**
1. **Canal de despacho en background (outbox en memoria + hosted service):** introducir `INodePushQueue` que encola `(nodeId, pushKind)`. Los endpoints encolan y retornan `202 Accepted` de inmediato.
   ```csharp
   public interface INodePushQueue
   {
       void Enqueue(NodePushRequest request); // { NodeId, Kind: Config|Rules|Pacs }
   }
   ```
2. **`NodePushDispatchHostedService`** consume el canal y ejecuta el push real en un scope propio, con el patrón supervisor de P0-8 (no mata el host).
3. **Paralelizar el fan-out** con límite de concurrencia (`Parallel.ForEachAsync` con `MaxDegreeOfParallelism = 8`) en vez del `foreach` secuencial.
4. **Feedback al SPA por SignalR:** al completar (éxito/fallo) cada push, emitir `NodePushStatusChanged` por `EdgeHubNotificationHub` para que la UI muestre el estado sin bloquear.
5. **Coalescing:** si llegan varios cambios para el mismo `nodeId` en una ventana corta, colapsar a un solo push (debounce ~500ms) para no saturar un nodo durante edición intensa de reglas.

**Archivos:**
- Nuevo: `src/backend/Dicom.Edge.Hub.Application/NodeConfiguration/INodePushQueue.cs`
- Nuevo: `src/backend/Dicom.Edge.Hub.Infrastructure/HostedServices/NodePushDispatchHostedService.cs`
- `src/backend/Dicom.Edge.Hub.Application/Routing/NodeDicomRoutingRuleService.cs` (encolar en vez de await)
- `src/backend/Dicom.Edge.Hub.Application/Nodes/NodeService.cs`
- `src/backend/Dicom.Edge.Hub.Application/PacsServers/PacsServerService.cs` (usar la cola + paralelizar)
- `src/backend/Dicom.Edge.Hub.Api/Controllers/NodeConfigurationController.cs` (devolver 202)
- `src/backend/Dicom.Edge.Hub.Api/Hubs/EdgeHubNotificationHub.cs` (evento de estado)

**Tests:**
- El endpoint de CRUD de reglas responde < 200ms aunque el nodo destino esté caído.
- Con el nodo caído, el dispatcher reintenta/loggea y emite `NodePushStatusChanged` con estado de error.
- 50 nodos: el fan-out completa en paralelo (≤ p95 30s objetivo del KPI P0) en vez de ~2.5 min secuencial.
- Dos ediciones rápidas del mismo nodo → un solo push (coalescing).

**Rollback:** Feature flag `Push:Async=false` que vuelve al `await` inline. Sin cambio de esquema.

**Riesgo en deploy:** **BAJO.** El contrato del nodo no cambia. El único cambio observable es que el SPA ya no espera la confirmación del nodo (que pasa a llegar por SignalR).

---

### P1-2 · Plumbing de `MaxClients` (y timeouts) al SCP de fo-dicom

**Problema:** `DicomServerOptions.MaxClients = 10` existe pero **nunca se pasa** a fo-dicom. En `DicomServerHostedService.StartAsync` la llamada es:
```csharp
_server = dicomServerFactory.Create<CStoreScp>(
    port: opts.Port, tlsAcceptor: tlsAcceptor, userState: ...);
```
No se usa el parámetro `maxClientsAllowed` (verificado: `maxClientsAllowed` no aparece en todo `src/edge`). Un atacante en la red del nodo puede abrir asociaciones ilimitadas y agotar threads/memoria → **DoS trivial**. Igualmente `AssociationTimeoutSeconds`, `DimseTimeoutSeconds` y `MaxPduLength` no se propagan.

**Solución:**
1. Pasar `maxClientsAllowed` en la factory:
   ```csharp
   _server = dicomServerFactory.Create<CStoreScp>(
       port: opts.Port,
       tlsAcceptor: tlsAcceptor,
       userState: deps,
       maxClientsAllowed: opts.MaxClients);
   ```
2. Aplicar los timeouts/PDU vía `_server.Options` tras la creación:
   ```csharp
   _server.Options.MaxPDULength       = opts.MaxPduLength;
   _server.Options.MaxClientsAllowed  = opts.MaxClients; // redundante pero explícito
   ```
   y los timeouts DIMSE/asociación donde fo-dicom los exponga (`DicomServiceOptions`).
3. Loggear el límite efectivo en el mensaje de arranque (ya hay log de arranque; añadir `MaxClients`).

**Archivos:**
- `src/edge/Dicom.Edge.Node.DicomServer/DicomServerHostedService.cs`

**Tests:**
- Abrir `MaxClients + 5` asociaciones concurrentes → las que exceden el límite son rechazadas/encoladas, no tumban el proceso.
- C-STORE normal sigue funcionando con el límite por defecto (10).

**Rollback:** Revert directo. Sin cambio de configuración ni esquema.

**Riesgo en deploy:** **MUY BAJO.** Si `MaxClients` por defecto (10) resultara bajo para un sitio con muchas modalidades simultáneas, subirlo por config (`DicomServer:MaxClients`). Recomendación: validar el pico real de asociaciones concurrentes por sitio antes de bajar el límite.

---

## FASE 2 — Correctitud bajo concurrencia

### P1-3 · Resolver race conditions en creación de Study/Patient/Series

**Problema:** En `EdgeNodeService.ProcessStudyNotifyAsync` (y `ProcessStudyProgressAsync`) el patrón es **read-then-create sin protección**:
```csharp
var existing = await studyRepository.GetByStudyInstanceUidAsync(request.StudyInstanceUid, ct); // línea 123
...
if (existing is not null) { /* update */ }
var study = Study.Create(...);            // línea 164
await studyRepository.AddAsync(study, ct);
await unitOfWork.SaveChangesAsync(ct);    // línea 177
```
Dos notificaciones concurrentes para el mismo `StudyInstanceUid` (dos nodos, reintentos, o `notify`+`progress` solapados) hacen **ambas** el miss en la línea 123 y **ambas** intentan crear:

- **Study:** `StudyInstanceUid` **tiene índice único** (`StudyConfiguration` línea 38) → la segunda transacción lanza `DbUpdateException` (violación de unicidad) **no capturada** → `500` al nodo y la notificación se pierde (o el nodo reintencia ciegamente).
- **Patient:** `PatientDicomId` tiene índice **NO único** (`PatientConfiguration` línea 38) → se crean **pacientes duplicados** silenciosamente. (En `Hl7PatientSyncService`.) **Es el caso más grave.**
- **Series:** `Study.AddSeries` deduplica en memoria, pero dos agregados Study cargados en paralelo pueden duplicar series.

**Solución:**
1. **Patient: añadir índice único** en `PatientDicomId.Value` (migración EF) — cerrar el agujero de duplicados a nivel DB.
2. **Patrón upsert con reintento ante conflicto** (helper compartido):
   ```csharp
   for (var attempt = 0; attempt < 2; attempt++)
   {
       var existing = await repo.GetByKeyAsync(key, ct);
       if (existing is not null) { /* update path */ return; }
       try
       {
           await repo.AddAsync(Create(...), ct);
           await uow.SaveChangesAsync(ct);
           return;
       }
       catch (DbUpdateException ex) when (IsUniqueViolation(ex))
       {
           // Otro hilo ganó la carrera → recargar y reintentar por el update path
       }
   }
   ```
   `IsUniqueViolation` detecta `PostgresException.SqlState == "23505"`.
3. Aplicar el patrón en `ProcessStudyNotifyAsync`, `ProcessStudyProgressAsync` y `Hl7PatientSyncService`.
4. **Opcional (defensa adicional):** lock keyed en memoria (`SemaphoreSlim` por `StudyInstanceUid`) para serializar dentro de una misma instancia del Hub y reducir el número de conflictos a DB. No sustituye al guard de DB (no protege entre instancias).

**Archivos:**
- `src/backend/Dicom.Edge.Hub.Persistence/Configurations/PatientConfiguration.cs` (índice único)
- Nueva migración EF: `AddPatientDicomIdUniqueIndex`
- `src/backend/Dicom.Edge.Hub.Application/Edge/EdgeNodeService.cs`
- `src/backend/Dicom.Edge.Hub.Application/Hl7/Hl7PatientSyncService.cs`
- Nuevo helper: `src/backend/Dicom.Edge.Hub.Persistence/UnitOfWork/UniqueViolationExtensions.cs`

**Tests:**
- 20 notificaciones concurrentes del mismo `StudyInstanceUid` → exactamente **1** Study en DB, 0 errores 500.
- 20 syncs concurrentes del mismo `PatientDicomId` → exactamente **1** Patient.
- La migración del índice único falla de forma controlada si ya existen duplicados (script previo de detección/merge).

**Rollback:** El patrón upsert es backward-compatible. La migración del índice único requiere `pg_dump` previo; rollback con `dotnet ef database update <previous>`. **Pre-requisito:** detectar y resolver duplicados de Patient existentes antes de aplicar el índice único:
```sql
SELECT patient_dicom_id, COUNT(*) FROM patients
GROUP BY patient_dicom_id HAVING COUNT(*) > 1;
```

**Riesgo en deploy:** **MEDIO.** La migración del índice único puede fallar si hay duplicados históricos. Mitigación: ejecutar el script de detección y un merge manual/asistido antes de migrar.

---

### P1-5 · Validar transiciones de la máquina de estados del Study

**Problema:** Los métodos de `Study` (`MarkCompleted`, `MarkQueuedForPacs`, `MarkSendingToPacs`, `MarkSentToPacs`, `MarkFailed`, `MergeFromDicom`…) cambian `Status` **sin validar que la transición sea legal**. Ejemplos de transiciones inválidas hoy posibles:

- `MarkSentToPacs()` desde `Receiving` (nunca pasó por `Sending`).
- `MarkCompleted()` sobre un estudio ya `SentToPacs` (retrocede el ciclo de vida).
- `MergeFromDicom()` sobre un estudio que ya no está `Scheduled`.

Esto produce auditorías de estado incoherentes (`StudyStatusAudit`), métricas falsas y reglas de negocio (envío a PACS, notificaciones) disparadas en el orden equivocado.

**Solución:**
1. **Tabla de transiciones permitidas** en el dominio:
   ```csharp
   private static readonly IReadOnlyDictionary<StudyStatus, StudyStatus[]> Allowed = new Dictionary<...>
   {
       [StudyStatus.Scheduled]     = [StudyStatus.Receiving, StudyStatus.Cancelled],
       [StudyStatus.Receiving]     = [StudyStatus.Completed, StudyStatus.Failed],
       [StudyStatus.Completed]     = [StudyStatus.QueuedForSend],
       [StudyStatus.QueuedForSend] = [StudyStatus.Sending, StudyStatus.Failed],
       [StudyStatus.Sending]       = [StudyStatus.SentToPacs, StudyStatus.Failed],
       [StudyStatus.Failed]        = [StudyStatus.QueuedForSend], // reintento
       [StudyStatus.SentToPacs]    = [],
   };
   ```
2. **Guard central** `TransitionTo(next, nodeId, reason)` que valida contra `Allowed`, lanza `InvalidStudyTransitionException` si no es legal, y centraliza `RecordStatusChange` + `CurrentStatusSince`:
   ```csharp
   private void TransitionTo(StudyStatus next, string? nodeId, string? reason)
   {
       if (!Allowed[Status].Contains(next))
           throw new InvalidStudyTransitionException(Id, Status, next);
       var old = Status; Status = next; CurrentStatusSince = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow;
       RecordStatusChange(old, next, nodeId, reason);
   }
   ```
3. Reescribir cada `Mark*` para delegar en `TransitionTo`. Mantener idempotencia donde aplique (p.ej. `MarkCompleted` sobre `Completed` → no-op, no excepción).
4. Decidir política ante transición ilegal en la capa de aplicación: loggear + descartar la notificación fuera de orden (no propagar 500 al nodo) — alinear con P1-3.

**Archivos:**
- `src/backend/Dicom.Edge.Hub.Domain/Aggregates/Studies/Study.cs`
- Nuevo: `src/backend/Dicom.Edge.Hub.Domain/Aggregates/Studies/InvalidStudyTransitionException.cs`
- `src/backend/Dicom.Edge.Hub.Application/Edge/EdgeNodeService.cs` (capturar la excepción, no 500)
- `src/shared/Dicom.Edge.Models/Enums/StudyStatus.cs` (verificar valores `Cancelled`, etc.)

**Tests:**
- Cada transición legal de la tabla → permitida.
- `Receiving → SentToPacs` → `InvalidStudyTransitionException`.
- `MarkCompleted` sobre estudio ya `Completed` → no-op idempotente.
- Notificación fuera de orden desde un nodo → loggea y no rompe (no 500).

**Rollback:** Revert directo del guard (vuelve a permitir cualquier transición). Sin migración.

**Riesgo en deploy:** **MEDIO.** Si el flujo real produce hoy alguna transición "ilegal" pero benigna, el guard podría empezar a rechazarla. Mitigación: primero desplegar en modo `WarnOnly` (loggea la transición ilegal pero la permite) durante 1 semana, revisar el log, y luego activar el modo bloqueante.

---

## Pre-requisito transversal: proyecto de tests

Hoy **no existe ningún proyecto de tests** en la solución. Todos los "Tests" de este plan (y del P0) necesitan dónde vivir. Antes/junto con la Fase 1, crear:

- `tests/Dicom.Edge.Hub.Domain.Tests` — máquina de estados (P1-5), merges.
- `tests/Dicom.Edge.Hub.Application.Tests` — concurrencia (P1-3), push async (P1-1).
- `tests/Dicom.Edge.Node.Sender.Tests` — retry/anonimización (P1-4).

Sugerido: xUnit + Testcontainers (Postgres) para los tests de concurrencia/migraciones.

---

## Métricas de Éxito

| Métrica | Antes | Objetivo |
|---|---|---|
| Latencia p95 de guardar una regla de ruteo en el SPA | hasta timeout del nodo (~30s) | < 300ms (push asíncrono) |
| Propagación de cambio admin → 50 nodos | ~2.5 min secuencial | ≤ 30s p95 (paralelo) |
| Asociaciones DICOM concurrentes antes de degradar | ilimitadas (DoS) | acotadas a `MaxClients` |
| Estudios duplicados por notificación concurrente | posible (500/duplicado) | 0 |
| Pacientes duplicados por sync concurrente | posible (silencioso) | 0 (índice único) |
| Transiciones de estado ilegales registradas | sin control | 0 (o loggeadas en WarnOnly) |

---

## Aprobaciones Requeridas

| Rol | Responsabilidad | Aprobado |
|---|---|---|
| Tech Lead | Diseño técnico P1-1…P1-5 | ☐ |
| DevOps | Migración índice único Patient + plan rollback | ☐ |
| QA Lead | Tests de concurrencia y máquina de estados | ☐ |
| Product Owner | Modo WarnOnly de la state machine (P1-5) | ☐ |
