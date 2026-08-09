# Plan de implementación — Bitácora independiente por asociación DICOM

> Plan técnico del diseño cotizado en
> [COT-2026-08-bitacora-por-asociacion.md](../cotizaciones/COT-2026-08-bitacora-por-asociacion.md).
> Rama sugerida: `feat/per-association-logging`.

---

## 0. Principios de diseño

1. **Aditivo.** El log global (`logs/node-YYYYMMDD.log`) no cambia: mismos sinks, enrichers y
   formato. La bitácora por asociación es un sink adicional.
2. **Sin cambios de firma en el pipeline.** El etiquetado viaja por contexto ambiental
   (`AsyncLocal`), igual que el `CorrelationScope` existente, para no propagar un
   `associationId` por parámetro a `IDicomInstanceHandler`, handlers de C-FIND, storage y EF.
3. **Cierre determinista.** El archivo se abre en `A-ASSOCIATE-RQ` y se cierra en
   release / abort / connection-closed. No se depende de evicción LRU ni de GC.
4. **Sin dependencias nuevas.** Todo se construye sobre `Serilog` + `Serilog.Sinks.File`, ya
   referenciados en [Dicom.Edge.Diagnostics.csproj](../../src/shared/Dicom.Edge.Diagnostics/Dicom.Edge.Diagnostics.csproj).
5. **Apagable en caliente.** `Diagnostics:File:PerAssociation:Enabled=false` deja el
   comportamiento actual bit a bit.
6. **PHI por el mismo camino.** El nuevo sink está *después* de `PhiRedactionEnricher` en el
   mismo pipeline; no existe una ruta sin redactar.

---

## 1. Inventario de archivos

### 1.1 Nuevos

| # | Archivo | Proyecto |
|---|---|---|
| N1 | `Correlation/AssociationLogContext.cs` | Dicom.Edge.Diagnostics |
| N2 | `Correlation/DicomAssociationContext.cs` | Dicom.Edge.Diagnostics |
| N3 | `Enrichers/AssociationEnricher.cs` | Dicom.Edge.Diagnostics |
| N4 | `Logging/AssociationScopedLogger.cs` | Dicom.Edge.Diagnostics |
| N5 | `Logging/IAssociationLogWriter.cs` | Dicom.Edge.Diagnostics |
| N6 | `Logging/AssociationFileLogWriter.cs` (sink + writer) | Dicom.Edge.Diagnostics |
| N7 | `Logging/AssociationSummary.cs` | Dicom.Edge.Diagnostics |
| N8 | `Logging/AssociationLogCleanupService.cs` | Dicom.Edge.Diagnostics |

### 1.2 Modificados

| # | Archivo | Cambio |
|---|---|---|
| M1 | [DiagnosticsConstants.cs](../../src/shared/Dicom.Edge.Diagnostics/Constants/DiagnosticsConstants.cs) | Constantes `AssociationId`, `RemoteHost`, `RemotePort`, `DimseOperation` |
| M2 | [DiagnosticsOptions.cs](../../src/shared/Dicom.Edge.Diagnostics/Configuration/DiagnosticsOptions.cs) | `FileLoggingOptions.PerAssociation` + clase `PerAssociationLoggingOptions` |
| M3 | [DiagnosticsOptionsValidator.cs](../../src/shared/Dicom.Edge.Diagnostics/Configuration/DiagnosticsOptionsValidator.cs) | Validación de la nueva sección |
| M4 | [PlatformDiagnosticsExtensions.cs](../../src/shared/Dicom.Edge.Diagnostics/Extensions/PlatformDiagnosticsExtensions.cs) | Registrar writer (singleton), enricher y sink; hosted service de limpieza |
| M5 | [CStoreScp.cs](../../src/edge/Dicom.Edge.Node.DicomServer/CStoreScp.cs) | Ciclo de vida de la bitácora + contexto por callback + detalle DIMSE |
| M6 | [DicomServerHostedService.cs](../../src/edge/Dicom.Edge.Node.DicomServer/DicomServerHostedService.cs) | `IAssociationLogWriter` en `DicomScpDependencies` |
| M7 | [DicomInstanceHandler.cs](../../src/edge/Dicom.Edge.Node/DicomInstanceHandler.cs) | Métricas por instancia (bytes, ms) en los logs existentes |
| M8 | [Program.cs](../../src/edge/Dicom.Edge.Node/Program.cs) | Nada si el registro va dentro de `AddPlatformDiagnostics` (verificar orden) |
| M9 | `appsettings.json` (Node) | Sección `PerAssociation` |
| M10 | `NodeSettingKeys` + `ConfigPaths` + [NodeSettingConfigPathMap.cs](../../src/edge/Dicom.Edge.Node.Persistence/Configuration/NodeSettingConfigPathMap.cs) | Llaves `diagnostics.assoc_log_*` para push desde el Hub |
| M11 | `docs/07-operations/monitoring.md`, `troubleshooting.md` | Uso en soporte y homologación |

---

## 2. Diseño detallado por componente

### 2.1 N1 · `AssociationLogContext`

Clase **mutable** (no `record` inmutable) porque el `AssociationId` se genera en el
constructor del `CStoreScp`, cuando todavía no se conocen los AE ni el host — que llegan en
`OnReceiveAssociationRequestAsync`. El adaptador de logger captura la referencia, de modo que
al completarse los datos todos los eventos posteriores ya salen enriquecidos.

```csharp
public sealed class AssociationLogContext(string associationId)
{
    public string AssociationId { get; } = associationId;
    public string CallingAe  { get; set; } = "?";
    public string CalledAe   { get; set; } = "?";
    public string RemoteHost { get; set; } = "?";
    public int    RemotePort { get; set; }
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;
}
```

### 2.2 N2 · `DicomAssociationContext`

```csharp
public static class DicomAssociationContext
{
    private static readonly AsyncLocal<AssociationLogContext?> _current = new();
    public static AssociationLogContext? Current => _current.Value;

    public static IDisposable Enter(AssociationLogContext ctx) => new Scope(ctx);

    private sealed class Scope : IDisposable   // restaura el valor previo (anidable)
    { ... }
}
```

Se abre **al inicio de cada callback** del SCP. Motivo (riesgo principal del proyecto):
fo-dicom invoca los callbacks desde su read-loop; un `AsyncLocal` fijado en
`OnReceiveAssociationRequestAsync` **no fluye** hacia `OnCStoreRequestAsync`. La regla es:
*una asociación = un contexto; cada callback lo re-entra.*

### 2.3 N3 · `AssociationEnricher`

```csharp
public sealed class AssociationEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent e, ILogEventPropertyFactory f)
    {
        var ctx = DicomAssociationContext.Current;
        if (ctx is null) return;
        e.AddPropertyIfAbsent(f.CreateProperty(DiagnosticsConstants.AssociationId, ctx.AssociationId));
        e.AddPropertyIfAbsent(f.CreateProperty(DiagnosticsConstants.CallingAeTitle, ctx.CallingAe));
        e.AddPropertyIfAbsent(f.CreateProperty(DiagnosticsConstants.CalledAeTitle,  ctx.CalledAe));
        e.AddPropertyIfAbsent(f.CreateProperty(DiagnosticsConstants.RemoteHost,     ctx.RemoteHost));
    }
}
```

Se registra en `ConfigureEnrichers` **antes** de `PhiRedactionEnricher` (orden de la lista
actual: `FromLogContext` → machine/process/thread → `InstanceEnricher` →
`CorrelationIdEnricher` → **`AssociationEnricher`** → `PhiRedactionEnricher`).

### 2.4 N4 · `AssociationScopedLogger`

Decorador `Microsoft.Extensions.Logging.ILogger` que re-entra el contexto alrededor de cada
`Log(...)`. Cubre los eventos **internos de fo-dicom** (PDU, DIMSE, timeouts), que se emiten
en el read-loop y por tanto no heredan el `AsyncLocal` de nuestros callbacks.

```csharp
internal sealed class AssociationScopedLogger(ILogger inner, AssociationLogContext ctx) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);
    public bool IsEnabled(LogLevel level) => inner.IsEnabled(level);

    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex,
                            Func<TState, Exception?, string> formatter)
    {
        using var _ = DicomAssociationContext.Enter(ctx);
        inner.Log(level, id, state, ex, formatter);
    }
}
```

`FellowOakDicom.Network.DicomService.Logger` es **settable** (verificado en fo-dicom 5.2.6),
así que se asigna en el constructor del `CStoreScp`.

### 2.5 N5/N6 · Writer + sink

```csharp
public interface IAssociationLogWriter
{
    void Open(AssociationLogContext ctx);                       // idempotente
    void Close(string associationId, AssociationSummary summary); // idempotente
    void SweepStale(TimeSpan ttl);                              // huérfanas
}
```

`AssociationFileLogWriter : IAssociationLogWriter, ILogEventSink, IDisposable` (singleton):

- Estado: `ConcurrentDictionary<string, Entry>`; `Entry = (Serilog.Core.Logger Logger, string Path, DateTime OpenedAt, int Events)`.
- `Emit(LogEvent e)`: si `e.Properties["AssociationId"]` existe y hay `Entry` → `entry.Logger.Write(e)`. Si no, **ignora** (el evento sigue yendo al log global). Coste: un lookup por evento.
- `Open(ctx)`: resuelve `IOptionsMonitor<DiagnosticsOptions>` (permite apagado en caliente),
  crea el directorio del día, construye
  `new LoggerConfiguration().MinimumLevel.Is(level).WriteTo.File(path, outputTemplate|CompactJsonFormatter, buffered:false, shared:false, fileSizeLimitBytes: MaxFileSizeMb).CreateLogger()`
  y escribe el **encabezado**.
- `Close(id, summary)`: escribe el **pie** (resumen), quita del diccionario y `Dispose()` del
  logger → flush + cierre del handle.
- `SweepStale(ttl)`: cierra entradas con `OpenedAt` anterior a `now - ttl`
  (`ttl = 2 × DimseTimeoutSeconds`, default 20 min) marcándolas como `Orphaned`.
- Topes: `MaxOpen` (default `MaxClients × 2`) y `MaxFilesPerDay`; al superarlos no se abre
  archivo y se emite **un** warning por minuto en el log global (evita log-storm).

**El sink NO se envuelve en `WriteTo.Async`**: se requiere orden estricto entre
`Emit` y `Close` para que ningún evento llegue después del cierre. Además `buffered:false`
en el archivo, porque el caso que más importa en homologación es justo el **abort** (proceso
o conexión que muere y el archivo debe estar completo en disco).

### 2.6 N7 · `AssociationSummary`

Contadores acumulados por el `CStoreScp` (todos `Interlocked`):
`CStoreOk`, `CStoreFailed`, `BytesReceived`, `CFindMwl`, `CFindQr`, `CFindResults`,
`CEcho`, `Errors`, `Status` (`Accepted`/`Rejected`/`Completed`/`Aborted`/`Closed`),
`RejectionReason`, `Duration`.

### 2.7 N8 · `AssociationLogCleanupService`

`BackgroundService` con ciclo de 1 h (mismo patrón que
[StudyCleanupService.cs](../../src/edge/Dicom.Edge.Node.Persistence/Services/StudyCleanupService.cs)):

1. `writer.SweepStale(ttl)`.
2. Borra carpetas de día anteriores a `RetainDays`.
3. Si el directorio raíz supera `MaxTotalSizeMb`, borra los días más antiguos hasta cumplir.
4. Log de resumen en el log global (archivos borrados, MB liberados).

### 2.8 M4 · Registro en el pipeline

En `AddPlatformDiagnostics`:

```csharp
services.AddSingleton<AssociationFileLogWriter>();
services.AddSingleton<IAssociationLogWriter>(sp => sp.GetRequiredService<AssociationFileLogWriter>());
if (options.File.PerAssociation.Enabled)
    services.AddHostedService<AssociationLogCleanupService>();
```

En `ConfigureSinks` (la lambda de `AddSerilog`/`UseSerilog` ya recibe `IServiceProvider`):

```csharp
var writer = services.GetService<AssociationFileLogWriter>();
if (writer is not null && options.File.PerAssociation.Enabled)
    loggerConfig.WriteTo.Sink(writer, restrictedToMinimumLevel: perAssoc.MinimumLevel);
```

> **Verificar en F1:** `ConfigureSinks` hoy recibe solo `DiagnosticsOptions`; hay que pasarle
> el `IServiceProvider` que ya está disponible en ambas sobrecargas de `UsePlatformLogging`.
> El writer es singleton y no depende de logging → no hay ciclo en la resolución.

### 2.9 M5 · `CStoreScp`

```csharp
private readonly AssociationLogContext _logCtx;
private readonly AssociationSummary    _summary = new();
private int _closed;

public CStoreScp(INetworkStream stream, Encoding fallbackEncoding, ILogger logger,
                 DicomServiceDependencies dependencies)
    : base(stream, fallbackEncoding, logger, dependencies)
{
    _logCtx = new AssociationLogContext(Guid.NewGuid().ToString("N")[..8]);
    Logger  = new AssociationScopedLogger(logger, _logCtx);   // captura internos de fo-dicom
}
```

Cambios por callback:

| Callback | Acción |
|---|---|
| `OnReceiveAssociationRequestAsync` | `Enter(_logCtx)`; completar AEs/host/puerto en `_logCtx`; `Deps.AssocLog.Open(_logCtx)` **antes** de validar (para capturar rechazos); en cada rechazo: `_summary.Status = Rejected` + `CloseLog()`; tras aceptar: log de contextos negociados con transfer syntax |
| `OnCStoreRequestAsync` | `Enter(_logCtx)`; `Stopwatch`; log de entrada (SOP Class/Instance, Study/Series, TS) y de salida (estatus DICOM, ms, bytes); contadores |
| `OnCFindRequestAsync` | `Enter(_logCtx)`; log de llaves de consulta (nivel + tags no-PHI), contador de resultados y estatus final con ms |
| `OnCEchoRequestAsync` | `Enter(_logCtx)`; contador |
| `OnReceiveAssociationReleaseRequestAsync` | `Enter(_logCtx)`; `_summary.Status = Completed`; `CloseLog()` **después** de `_session.CompleteAsync()` |
| `OnReceiveAbort` | `_summary.Status = Aborted` + razón; `CloseLog()` |
| `OnConnectionClosed` | `CloseLog()` (idempotente vía `Interlocked.Exchange(ref _closed, 1)`) |

Además: sustituir los ~20 usos de `Deps.Logger` por el `Logger` de la instancia (ya
decorado). Es un reemplazo mecánico; el tipo sigue siendo `ILogger` de MEL, así que todas las
extensiones `LogInformation/LogWarning/...` funcionan igual.

`OnCFindRequestAsync` es un **iterador asíncrono**: el `using var _ = Enter(...)` se coloca en
el cuerpo del método y hay que **verificar explícitamente** (F3) que los eventos emitidos
entre `yield return` conservan el contexto en la máquina de estados.

---

## 3. Formato del archivo

`logs/associations/2026-08-09/20260809-143012.481_CT-SIEMENS_10.0.0.21_a1b2c3d4.log`

Nombre = `{ts:yyyyMMdd-HHmmss.fff}_{CallingAE saneado}_{IP}_{AssociationId}.log`
(caracteres inválidos de Windows → `_`; AE truncado a 16).

```
════════════════════════════════════════════════════════════════════════
 ASSOCIATION a1b2c3d4
 Started    : 2026-08-09 14:30:12.481 UTC
 Calling AE : CT-SIEMENS      Remote: 10.0.0.21:51042
 Called  AE : EDGE-NODE-01    Node  : NODE-001 (EdgeGuardNode 1.0.0)
════════════════════════════════════════════════════════════════════════
[14:30:12.482] [INF] Association request — CallingAE=CT-SIEMENS CalledAE=EDGE-NODE-01 ...
[14:30:12.489] [INF] Equipment matched — Id=42 Enabled=true IpMatch=true
[14:30:12.491] [DBG] PC 1 ACCEPTED  1.2.840.10008.5.1.4.1.1.2 (CT Image Storage) TS=1.2.840.10008.1.2.1
[14:30:12.492] [DBG] PC 3 REJECTED  1.2.840.10008.5.1.4.1.1.88.67 (not supported)
[14:30:12.494] [INF] Association negotiation complete — Accepted=2 Rejected=1
[14:30:12.501] [INF] C-STORE #1 begin — SOP=1.2.826...4501 Study=1.2.826...900 Series=...
[14:30:12.612] [INF] C-STORE #1 end   — Status=Success 524288 bytes in 111 ms → /data/studies/...
...
[14:31:02.004] [INF] C-FIND MWL — level=PATIENT keys=[Modality=CT, ScheduledDate=20260809] → 3 results in 12 ms
[14:31:40.118] [INF] Association release requested
────────────────────────────────────────────────────────────────────────
 SUMMARY  status=Completed  duration=87.6s
 C-STORE  ok=120  failed=0  bytes=62.9 MB
 C-FIND   mwl=1 (3 results)  qr=0        C-ECHO 0     errors=0
════════════════════════════════════════════════════════════════════════
```

Con `UseCompactJson=true` el mismo contenido sale como JSON por línea (ingesta en Seq/ELK),
sin encabezado/pie ASCII (se emiten como dos eventos estructurados `AssociationOpened` /
`AssociationClosed`).

---

## 4. Configuración

### 4.1 `appsettings.json` (Node) — M9

```jsonc
"Diagnostics": {
  "File": {
    "Path": "logs",
    "FileNameTemplate": "node-.log",
    "PerAssociation": {
      "Enabled": true,
      "Path": "logs/associations",
      "MinimumLevel": "Debug",
      "UseCompactJson": false,
      "RetainDays": 14,
      "MaxFilesPerDay": 5000,
      "MaxFileSizeMb": 10,
      "MaxTotalSizeMb": 2048,
      "MaxOpen": 20,
      "IncludeFoDicomInternals": true
    }
  }
}
```

### 4.2 Push desde el Hub — M10

Cadena existente **DB → env → appsettings** vía
[NodeSettingConfigPathMap.cs](../../src/edge/Dicom.Edge.Node.Persistence/Configuration/NodeSettingConfigPathMap.cs).
Se agregan llaves y sus `ConfigPaths`:

| `node_settings` key | ConfigPath |
|---|---|
| `diagnostics.assoc_log_enabled` | `Diagnostics:File:PerAssociation:Enabled` |
| `diagnostics.assoc_log_level` | `Diagnostics:File:PerAssociation:MinimumLevel` |
| `diagnostics.assoc_log_retain_days` | `Diagnostics:File:PerAssociation:RetainDays` |

El writer lee `IOptionsMonitor<DiagnosticsOptions>` en cada `Open`, así que activar/desactivar
desde el Hub surte efecto en la **siguiente asociación**, sin reiniciar el servicio.

---

## 5. Fases de implementación

Cada fase deja el repositorio compilando y desplegable. Recordatorio de
[Directory.Build.props](../../Directory.Build.props): CS9113 (parámetro de constructor
primario sin usar) y CS1998 (async sin await) son **errores** de compilación.

> **Estado:** F1, F2 y F3 implementadas y verificadas en la rama
> `feat/per-association-logging` (ver §6.1). Pendientes: F4, F5, F6.

### F1 — Cimientos de diagnóstico (N1–N4, M1–M4) · ~1.5 JID · ✅ hecha
- Contexto, enricher, decorador de logger, opciones y validación.
- Sink registrado pero **sin** ningún productor todavía.
- **Aceptación:** el nodo arranca; `logs/node-*.log` idéntico; ningún archivo en
  `logs/associations` (nadie llama `Open`); `Enabled=false` no altera nada.

### F2 — Writer + sink de archivo (N5–N7) · ~2.0 JID · ✅ hecha
- Apertura/cierre determinista, naming, encabezado/pie, topes, `SweepStale`.
- **Aceptación:** prueba manual con un `Open`/`Emit`/`Close` sintético (endpoint temporal o
  console runner en el scratchpad) genera el archivo, lo cierra y libera el handle
  (verificable con `handle.exe` o simplemente borrando el archivo sin error de bloqueo).

### F3 — Integración en el SCP (M5, M6) · ~1.5 JID · ✅ hecha
- Ciclo de vida completo, contexto por callback, sustitución de `Deps.Logger`.
- **Aceptación:** un `storescu` genera exactamente **un** archivo con todo el ciclo; un AE no
  registrado genera archivo con el rechazo y su motivo; un `Ctrl+C` a mitad de envío deja
  archivo cerrado con `status=Aborted`.
- **Verificación crítica:** contexto vivo dentro del iterador de `OnCFindRequestAsync` y en
  los logs internos de fo-dicom.

### F4 — Detalle DIMSE y resumen (M5, M7) · ~1.5 JID
- Logs por C-STORE (SOP/estatus/bytes/ms/ruta), por C-FIND (llaves, resultados, ms) y C-ECHO;
  contadores y pie de resumen.
- **Aceptación:** un estudio de N imágenes produce N pares begin/end y el resumen cuadra
  (`ok+failed = N`, bytes ≈ tamaño en disco).

### F5 — Retención, configuración y PHI (N8, M9, M10) · ~1.5 JID
- Servicio de limpieza, llaves de Hub, revisión de redacción en el nuevo canal.
- **Aceptación:** con `RetainDays=0` en una carpeta sembrada, el ciclo la elimina; un
  `PatientName` en un log de prueba sale `[REDACTED]` **también** en la bitácora.

### F6 — QA de campo y documentación (M11) · ~2.0 JID
- Matriz de la §6, ajustes finos de formato y documentación operativa.

---

## 6. Plan de verificación

### 6.1 Arnés de verificación de F1–F3 (ejecutado)

Como la solución no tiene proyecto de pruebas, F1–F3 se verificaron con un arnés que levanta
el **`CStoreScp` real** en un puerto local con dependencias falsas y lo ataca con SCU reales de
fo-dicom (3 asociaciones concurrentes de 5 C-STORE cada una, MWL, C-ECHO, rechazo por CalledAE
y abort forzado a media transferencia). **30/30 comprobaciones en verde**, entre ellas:

| Comprobación | Por qué importa |
|---|---|
| Un archivo por asociación, nombre con AE + IP + `AssociationId` | Requisito base |
| La línea del *instance handler* (otro logger, otro flujo async) cae en el archivo correcto | Prueba que el contexto ambiental se propaga hacia abajo |
| **Cero contaminación cruzada** entre 3 asociaciones simultáneas | Criterio principal del proyecto |
| Los eventos internos de fo-dicom (accept dump, C-STORE response, abort) quedan en el archivo | Prueba el decorador de `DicomService.Logger` |
| Rechazo por CalledAE genera archivo con `status=Rejected` y su motivo | Caso de homologación |
| Abort forzado deja archivo cerrado con `status=Aborted` y su razón | Cierre determinista |
| Todos los archivos terminan en pie `SUMMARY` y quedan sin bloqueo de handle | Sin fugas |
| El log global sigue en Information (sin `[DBG]`) mientras el archivo de asociación sí trae Debug | El log global no se degrada |
| `AssociationId=` aparece en el log global | Correlación entre ambos canales |

El arnés vive fuera del repositorio (directorio de scratchpad de la sesión); si se quiere
conservar como regresión, corresponde al opcional §6-E de la cotización.

### 6.2 QA de campo (F6)

Sin proyectos de test en la solución (no existe ningún `*.Tests.csproj` en
[EdgeGuard.Platform.slnx](../../EdgeGuard.Platform.slnx)), la verificación es con SCU reales
(dcm4che / DCMTK):

| # | Caso | Comando / acción | Resultado esperado |
|---|---|---|---|
| 1 | C-STORE simple | `storescu -aec EDGE-NODE-01 -aet CT-TEST host 11112 img.dcm` | 1 archivo, `status=Completed`, 1 C-STORE ok |
| 2 | Estudio multi-serie | `storescu ... +sd +r estudio/` | N pares begin/end, resumen cuadrado |
| 3 | MWL | `findscu -W -k 0008,0060=CT` | llaves + nº de resultados + ms |
| 4 | Q/R Study Root | `findscu -S -k 0008,0052=STUDY` | ídem |
| 5 | C-ECHO | `echoscu` | archivo mínimo con el C-ECHO |
| 6 | Rechazo por CalledAE | `storescu -aec MALO` | archivo con rechazo + motivo |
| 7 | Rechazo por AE/IP | equipo con IP distinta | archivo con `source IP mismatch` |
| 8 | Abort | matar el SCU a media transferencia | `status=Aborted`, archivo cerrado y completo |
| 9 | **Concurrencia** | 3 `storescu` simultáneos | 3 archivos **sin** eventos cruzados (criterio principal) |
| 10 | Flood | 200 asociaciones seguidas | topes respetados, sin fuga de handles ni de memoria |
| 11 | Apagado | `Enabled=false` + siguiente asociación | no se crea archivo; log global intacto |
| 12 | Retención | sembrar carpetas viejas | limpieza tras el ciclo |

Medición de overhead en el caso 2 (200 imágenes): comparar duración total con la bitácora
activa vs. apagada; **criterio de aceptación: < 5 % de diferencia**.

---

## 7. Casos borde y decisiones tomadas

| Tema | Decisión |
|---|---|
| Contexto que no fluye en fo-dicom | Doble mecanismo: `Enter()` por callback **+** logger decorado a nivel de instancia |
| Evento posterior al cierre | `Emit` sin entrada en el diccionario → se descarta (solo queda en el log global) |
| Doble cierre (abort + connection closed) | `Interlocked.Exchange` en `CStoreScp` + `Close` idempotente en el writer |
| Asociación que nunca cierra | `SweepStale(2 × DimseTimeout)` marca `status=Orphaned` |
| Rechazo antes de negociar | `Open` se ejecuta **antes** de validar AE/catálogo/IP |
| AE con caracteres inválidos para el FS | Saneado + truncado a 16 en el nombre; el AE crudo va en el encabezado |
| Colisión de nombres | Sufijo `AssociationId` (8 hex) garantiza unicidad |
| Disco lleno | Fallo al abrir → warning en log global y la asociación **continúa** (nunca se tira una asociación por un problema de logging); todo `Open/Emit/Close` va en `try/catch` |
| PHI | Mismo `PhiRedactionEnricher`; dumps crudos de dataset/PDU quedan fuera del alcance base |
| Rendimiento | Sink síncrono con lookup O(1); el log global sigue en `WriteTo.Async` |

---

## 8. Rollback

1. `Diagnostics:File:PerAssociation:Enabled=false` (appsettings o push del Hub) → el sink deja
   de abrir archivos; el resto del sistema no se entera.
2. La rama es aditiva salvo la sustitución de `Deps.Logger` → `Logger` en `CStoreScp`, que es
   neutra en comportamiento (mismo `ILoggerFactory` por debajo).
3. No hay migración de base de datos en el alcance base → revertir la rama no deja rastro.

---

## 9. Mapeo con la cotización

| Fase | Ítems de la cotización | JID |
|---|---|---:|
| F1 | 1, 5 (parcial) | 1.5 |
| F2 | 2 | 2.0 |
| F3 | 3 | 1.5 |
| F4 | 4 | 1.5 |
| F5 | 5, 6, 7 | 1.5 |
| F6 | 8, 9 | 2.0 |
| | **Subtotal** | **10.0** |
| | Gestión / buffer (~15%) | 1.5 |
| | **Total** | **11.5** |

Alcance mínimo (§5.1 de la cotización) = **F1 + F2 + F3** (5.0 JID) + PHI/config (0.5) →
5.5 JID; entrega el archivo por asociación con el detalle que hoy ya se loguea, dejando F4–F6
para una segunda fase.
