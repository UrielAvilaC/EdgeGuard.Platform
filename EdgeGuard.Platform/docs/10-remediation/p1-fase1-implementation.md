# Plan de Implementación — P1 Fase 1 (Estabilidad / DoS)

> **Estado:** Borrador — pendiente de aprobación
> **Fecha:** 2026-05-31
> **Alcance:** P1-1 (push asíncrono) y P1-2 (`MaxClients` en fo-dicom)
> **Esfuerzo:** ~1.5 días-persona · **Riesgo:** Bajo
> **Referencia:** `docs/10-remediation/p1-remediation-plan.md`

---

## 1. Objetivo y orden de ejecución

Cerrar dos problemas de Fase 1 sin tocar lógica de dominio:

1. **P1-2 primero** (~2-4h, aislado, sin dependencias) — plumbing de `MaxClients` al SCP.
2. **P1-1 después** (~1 día) — sacar el push de configuración del request del SPA.

Se hacen en **PRs separados** para minimizar blast radius y permitir rollback independiente.

---

## 2. Decisiones de diseño (P1-1)

| Decisión | Elección | Motivo |
|---|---|---|
| Dónde vive la cola (`INodePushQueue`) | **Application** (interfaz) + impl singleton sin dependencias | Los servicios de Application encolan; no deben conocer HTTP/SignalR. |
| Mecanismo de cola | `System.Threading.Channels.Channel<NodePushRequest>` (unbounded, single-reader) | Event-driven, sin polling; más simple que un outbox en DB para config (idempotente y reconstruible). |
| Dónde vive el consumidor | **`Dicom.Edge.Hub.Api`** (hosted service) | Es el único proyecto que referencia Application + Infrastructure **y** tiene acceso a `IHubContext<EdgeHubNotificationHub>`. Evita una dependencia Infrastructure→Api. |
| Notificación al SPA | SignalR `IHubContext<EdgeHubNotificationHub>` | Los helpers ya existen (`HubNotificationExtensions`) pero **hoy no se consumen**; este será su primer consumidor real. |
| Despacho por tipo | `NodePushKind { Config, Rules, Pacs }` → enruta al push service correspondiente | Unifica los 3 servicios (`INodeConfigPushService`, `INodeDicomRoutingRulePushService`, `INodePacsDestinationPushService`), todos con `PushAsync(nodeId, ct)`. |
| Concurrencia del fan-out | `Parallel.ForEachAsync`, `MaxDegreeOfParallelism = 8` | Reemplaza el `foreach … await` secuencial de `PacsServerService`. |
| Idempotencia / coalescing | Debounce por `nodeId` (~500ms) en el consumidor | Evita saturar un nodo durante edición intensa de reglas. |
| Feature flag | `Push:Async` (default `true`) | Permite rollback inmediato al comportamiento `await` inline. |

---

## 3. P1-2 · Plumbing de `MaxClients` (PR #1)

**Único archivo:** `src/edge/Dicom.Edge.Node.DicomServer/DicomServerHostedService.cs`

**Paso 1** — pasar el límite a la factory en `StartAsync`:
```csharp
_server = dicomServerFactory.Create<CStoreScp>(
    port:        opts.Port,
    tlsAcceptor: tlsAcceptor,
    userState:   deps,
    maxClientsAllowed: opts.MaxClients);   // ← antes: ausente (0 = ilimitado)
```

**Paso 2** — propagar timeouts/PDU tras crear el server:
```csharp
_server.Options.MaxPDULength               = opts.MaxPduLength;
_server.Options.LogDimseDatasets           = false;
// Timeouts DIMSE/asociación si la versión de fo-dicom los expone en DicomServiceOptions:
// _server.Options.RequestTimeout          = TimeSpan.FromSeconds(opts.DimseTimeoutSeconds);
```

**Paso 3** — incluir `MaxClients` en el log de arranque existente (línea ~95).

**Verificación de compilación:** confirmar la firma exacta de `IDicomServerFactory.Create<T>` en la versión de fo-dicom usada (`maxClientsAllowed` es el nombre del parámetro en fo-dicom 5.x).

**Tests (cuando exista el proyecto de tests):**
- Abrir `MaxClients + 5` asociaciones concurrentes → las excedentes se rechazan/encolan; el proceso no cae.
- C-STORE normal sigue funcionando con default (10).

**Rollback:** revert del archivo. Sin config ni esquema.

---

## 4. P1-1 · Push asíncrono (PR #2 + PR #3)

Se divide en dos PRs para que la infraestructura entre **sin cambiar comportamiento** y luego se haga el switch.

### PR #2 — Infraestructura de cola + consumidor (sin cambiar comportamiento)

**Nuevos archivos:**

1. `src/backend/Dicom.Edge.Hub.Application/NodeConfiguration/NodePushRequest.cs`
   ```csharp
   public enum NodePushKind { Config, Rules, Pacs }
   public sealed record NodePushRequest(string NodeId, NodePushKind Kind);
   ```

2. `src/backend/Dicom.Edge.Hub.Application/NodeConfiguration/INodePushQueue.cs`
   ```csharp
   public interface INodePushQueue
   {
       void Enqueue(NodePushRequest request);
       IAsyncEnumerable<NodePushRequest> DequeueAllAsync(CancellationToken ct);
   }
   ```

3. `src/backend/Dicom.Edge.Hub.Application/NodeConfiguration/NodePushQueue.cs`
   — singleton que envuelve `Channel.CreateUnbounded<NodePushRequest>(new(){ SingleReader = true })`.

4. `src/backend/Dicom.Edge.Hub.Api/HostedServices/NodePushDispatchHostedService.cs`
   — `BackgroundService` que consume el canal. Patrón supervisor de P0-8 (no mata el host). Por cada request:
   ```csharp
   protected override async Task ExecuteAsync(CancellationToken ct)
   {
       await foreach (var req in queue.DequeueAllAsync(ct))
       {
           try
           {
               using var scope = scopeFactory.CreateScope();
               var ok = await PushAsync(scope, req, ct);     // enruta por Kind
               await hub.NotifyNodePushStatus(req.NodeId,
                   new { req.NodeId, req.Kind, success = ok });
           }
           catch (OperationCanceledException) { break; }
           catch (Exception ex)
           {
               logger.LogError(ex, "Push dispatch failed for {NodeId}/{Kind}", req.NodeId, req.Kind);
               await hub.NotifyNodePushStatus(req.NodeId,
                   new { req.NodeId, req.Kind, success = false, error = ex.Message });
           }
       }
   }
   ```
   `PushAsync(scope, req, ct)` resuelve el servicio según `req.Kind` y llama `PushAsync(nodeId, ct)`.

**Modificaciones:**

5. `EdgeHubNotificationHub.cs` — añadir helper:
   ```csharp
   public static Task NotifyNodePushStatus(this IHubContext<EdgeHubNotificationHub> hub, string nodeId, object payload) =>
       Task.WhenAll(
           hub.Clients.Group("dashboard").SendAsync("NodePushStatus", payload),
           hub.Clients.Group($"node-{nodeId}").SendAsync("NodePushStatus", payload));
   ```

6. Registro DI:
   - `AddHubApplication` (o donde corresponda): `services.AddSingleton<INodePushQueue, NodePushQueue>();`
   - `Program.cs` o `HubHostedServicesExtensions`: `services.AddHostedService<NodePushDispatchHostedService>();`
     (El hosted service va en Api porque usa `IHubContext`.)

**Resultado de PR #2:** la cola existe y el consumidor corre, pero **nadie encola todavía** → cero cambio de comportamiento observable. Mergeable y desplegable solo.

### PR #3 — Switch de los call sites a `Enqueue` + flag + 202

**Feature flag** `Push:Async` (default `true`). Helper local en cada servicio:
```csharp
// si Push:Async == false → comportamiento legacy (await inline)
if (_pushAsync) pushQueue.Enqueue(new(nodeId, NodePushKind.Rules));
else            await pushService.PushAsync(nodeId, ct);
```

**Archivos a modificar:**

1. `src/backend/Dicom.Edge.Hub.Application/Routing/NodeDicomRoutingRuleService.cs`
   — reemplazar las 6 llamadas `await pushService.PushAsync(...)` (líneas 42, 70, 82, 94, 106, 121) por `pushQueue.Enqueue(...)`. Inyectar `INodePushQueue` + flag.

2. `src/backend/Dicom.Edge.Hub.Application/Nodes/NodeService.cs`
   — líneas 153, 174.

3. `src/backend/Dicom.Edge.Hub.Application/PacsServers/PacsServerService.cs`
   — reemplazar `PushPacsToAllNodesAsync` (el `foreach … await` secuencial) por encolar un request por nodo activo:
   ```csharp
   foreach (var node in nodes.Where(n => !string.IsNullOrWhiteSpace(n.ApiEndpoint)))
       pushQueue.Enqueue(new(node.Id, NodePushKind.Pacs));
   ```
   La paralelización real la da el consumidor (`Parallel.ForEachAsync` interno o varios lectores). Elimina el `_ = PushPacsToAllNodesAsync()` unobserved.

4. `src/backend/Dicom.Edge.Hub.Api/Controllers/NodeConfigurationController.cs`
   — el **push automático** por cambio de config → encolar.
   — el **endpoint manual `POST {nodeId}/push`** (línea 108): **mantener síncrono** (el usuario hace clic y espera el resultado), pero acotar con timeout. Documentar la distinción: automático = async; manual = síncrono.

**Coalescing (opcional, recomendado):** en `NodePushQueue.Enqueue`, deduplicar `(nodeId, Kind)` ya pendientes en una ventana de ~500ms (usar un `ConcurrentDictionary<(string,NodePushKind), DateTime>` como gate).

---

## 5. Plan de pruebas

| Caso | Resultado esperado |
|---|---|
| Guardar regla de ruteo con el nodo destino **caído** | El endpoint responde < 300ms (202/200); el dispatcher loggea fallo y emite `NodePushStatus{success:false}`. |
| Guardar regla con nodo **sano** | Endpoint rápido; SignalR emite `NodePushStatus{success:true}`; el nodo recibe la config. |
| Cambio de PACS con 50 nodos | Fan-out paralelo; todos encolados al instante; el SPA no se bloquea. |
| Dos ediciones del mismo nodo en < 500ms | Un solo push efectivo (coalescing). |
| `Push:Async=false` | Vuelve al `await` inline (comportamiento legacy). |
| `MaxClients + 5` asociaciones DICOM | Excedentes rechazadas; proceso estable (P1-2). |

**Pre-requisito:** crear `tests/Dicom.Edge.Hub.Api.Tests` (o `…Application.Tests`) — hoy no hay proyecto de tests. Mínimo: test del dispatcher con un `IHubContext` y push services fakeados.

---

## 6. Rollout y rollback

1. **Deploy PR #1 (MaxClients)** — independiente, sin riesgo de contrato.
2. **Deploy PR #2 (infra cola)** — sin cambio de comportamiento (nadie encola).
3. **Deploy PR #3 con `Push:Async=true`** — observar métricas de latencia del SPA y `NodePushStatus`.
4. **Rollback rápido:** flip `Push:Async=false` (sin redeploy si la flag es de config recargable) → vuelve al push inline.

**Sin migraciones de DB en toda la Fase 1.**

---

## 7. Checklist de tareas (orden de ejecución)

- [ ] **T1 (P1-2):** `DicomServerHostedService` — pasar `maxClientsAllowed` + PDU/timeouts + log. *(PR #1)*
- [ ] **T2:** `NodePushRequest` + `NodePushKind`. *(PR #2)*
- [ ] **T3:** `INodePushQueue` + `NodePushQueue` (Channel singleton). *(PR #2)*
- [ ] **T4:** `NodePushDispatchHostedService` en Api (supervisor + ruteo por Kind + SignalR). *(PR #2)*
- [ ] **T5:** `NotifyNodePushStatus` en `HubNotificationExtensions`. *(PR #2)*
- [ ] **T6:** Registro DI (singleton cola + hosted service). *(PR #2)*
- [ ] **T7:** Flag `Push:Async` + switch en `NodeDicomRoutingRuleService`. *(PR #3)*
- [ ] **T8:** Switch en `NodeService` y `PacsServerService` (encolar + eliminar fire-and-forget unobserved). *(PR #3)*
- [ ] **T9:** `NodeConfigurationController` — automático async, manual síncrono. *(PR #3)*
- [ ] **T10:** Coalescing/debounce en `Enqueue`. *(PR #3, opcional)*
- [ ] **T11:** Proyecto de tests + casos de la sección 5.
- [ ] **T12:** Documentar el evento SignalR `NodePushStatus` para el equipo de SPA.

---

## 8. Dependencias y notas para el SPA

- Nuevo evento SignalR **`NodePushStatus`** con payload `{ nodeId, kind, success, error? }`. El SPA debería suscribirse (grupo `dashboard` o `node-{id}`) y mostrar un toast/estado en vez de esperar la respuesta HTTP del push.
- Los endpoints de CRUD de reglas y de cambio de PACS pasan a responder de inmediato; la UI ya no debe asumir que "200 == nodo actualizado".
