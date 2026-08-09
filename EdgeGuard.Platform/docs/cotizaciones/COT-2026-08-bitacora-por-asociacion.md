# Cotización — Bitácora independiente por asociación DICOM

| Campo | Valor |
|---|---|
| **Folio** | COT-2026-08-LOGASSOC-01 |
| **Fecha** | 2026-08-09 |
| **Módulo** | EdgeGuard / Dicom.Edge.Node — DicomServer + Diagnostics |
| **Solicitante** | Uriel Avila |
| **Tipo** | Ajuste evolutivo (observabilidad / trazabilidad) |
| **Rama base sugerida** | `feat/per-association-logging` (a partir de `main`) |

---

## 1. Contexto y objetivo

### 1.1 Cómo se registra hoy

El nodo escribe **un solo log global** por día. La configuración vive en
[PlatformDiagnosticsExtensions.cs](src/shared/Dicom.Edge.Diagnostics/Extensions/PlatformDiagnosticsExtensions.cs)
(`ConfigureSinks`): un sink `File` con rolling diario (`logs/node-YYYYMMDD.log`,
CompactJson), más Console en Development y sinks opcionales Seq/HTTP. Los eventos se
enriquecen con `InstanceId`, `CorrelationId` (desde `Activity.Current` o
[CorrelationScope](src/shared/Dicom.Edge.Diagnostics/Correlation/CorrelationScope.cs)),
máquina, proceso y thread, y pasan por `PhiRedactionEnricher`.

El SCP es [CStoreScp.cs](src/edge/Dicom.Edge.Node.DicomServer/CStoreScp.cs): fo-dicom crea
**una instancia por asociación**, y ya existe un ciclo de vida completo
(`OnReceiveAssociationRequestAsync` → negociación de contextos → `OnCStoreRequestAsync` /
`OnCFindRequestAsync` / `OnCEchoRequestAsync` → `OnReceiveAssociationReleaseRequestAsync` /
`OnReceiveAbort` / `OnConnectionClosed`) con persistencia de la asociación en
`dicom_associations` vía
[DicomAssociationTracker.cs](src/edge/Dicom.Edge.Node/DicomAssociationTracker.cs).

**Limitación actual:** todo ese detalle queda **intercalado** en el log global con el resto
del nodo (sync con Hub, cola, sender, API). Con varias modalidades concurrentes
(`MaxClientsAllowed`), los eventos de una asociación quedan **entrelazados** con los de otras
y no hay ninguna llave que permita separarlos: no existe `AssociationId` en los logs, ni el
logger que usa el SCP es por-asociación (`Deps.Logger` es el singleton
`ILogger<DicomServerHostedService>`).

### 1.2 Objetivo

Además del log habitual (que **no cambia**), generar **un archivo independiente por
asociación DICOM**, que contenga la traza completa **desde que llega la petición de
asociación hasta que se libera** (release), se aborta o se cae la conexión:

- Petición de asociación: Calling/Called AE, host, puerto, timestamp.
- Validación (catálogo de equipos, AE+IP, whitelist) y **rechazo** con su motivo.
- Negociación de contextos de presentación (aceptados/rechazados, transfer syntax).
- **Cada C-STORE**: SOP Instance/Class UID, Study/Series, tamaño, estatus DICOM, duración,
  ruta de almacenamiento y resultado del guardado/registro en BD.
- **Cada C-FIND** (MWL y Study Root Q/R): llaves de consulta, número de resultados,
  estatus final y duración.
- **C-ECHO** y errores/excepciones ocurridos dentro de la asociación.
- Cierre: release / abort (fuente y razón) / connection closed, total de imágenes y duración.

---

## 2. Alcance funcional

### 2.1 Archivo por asociación

- Directorio nuevo `logs/associations/YYYY-MM-DD/`.
- Nombre determinista y ordenable, p. ej.
  `20260809-143012_CT-SIEMENS_10.0.0.21_a1b2c3d4.log`
  (timestamp de conexión · CallingAE saneado · IP · `AssociationId` corto).
- Formato configurable: texto plano legible (default, para soporte) o CompactJson
  (para ingesta en Seq/ELK), reutilizando el mismo `outputTemplate` del log global.
- Las asociaciones **rechazadas** también generan archivo (con el motivo del rechazo);
  es justo el caso que más se depura en homologación.
- Encabezado y pie del archivo con **resumen de la asociación**: AEs, host, contextos
  aceptados, nº de C-STORE ok/fallidos, nº de C-FIND y resultados, duración total y
  estatus final (Completed / Aborted / Rejected).

### 2.2 Cómo se etiqueta cada evento

Tres piezas nuevas en `Dicom.Edge.Diagnostics`:

1. **`DicomAssociationContext`** — contexto ambiental (`AsyncLocal`) con
   `AssociationId`, `CallingAe`, `CalledAe`, `RemoteHost`, `ConnectedAt`. Se abre al inicio
   de **cada callback** del SCP, de modo que todo lo que se ejecuta por debajo
   (`DicomInstanceHandler`, `WorklistCFindHandler`, `StudyRootCFindHandler`, escritura de
   archivo, EF Core) queda etiquetado **sin tocar sus firmas**. Mismo patrón que el
   `CorrelationScope` ya existente.
2. **`AssociationIdEnricher`** — enricher de Serilog que copia esas propiedades al
   `LogEvent` cuando hay contexto activo.
3. **`AssociationLoggerAdapter`** — decorador `ILogger` que se inyecta al
   `DicomService.Logger` del `CStoreScp` en su constructor. Cubre los eventos **internos de
   fo-dicom** (PDU, DIMSE, timeouts), que corren en el read-loop de la conexión y por tanto
   **no** heredan el `AsyncLocal`.

### 2.3 Enrutamiento a archivo

Sink propio `AssociationFileSink` (singleton) que mantiene un `ILogger` de Serilog por
asociación viva y enruta cada evento por su propiedad `AssociationId`:

- **Apertura determinista** al recibir la petición de asociación; **cierre y flush
  determinista** en release / abort / connection closed.
- Barrido periódico de asociaciones huérfanas (TTL) para que ningún handle quede abierto si
  fo-dicom no invoca el callback de cierre.
- Tope de archivos abiertos simultáneos (alineado con `MaxClients`) y límite de tamaño por
  archivo para acotar el impacto de un flood.

> Alternativa más barata evaluada: `Serilog.Sinks.Map` (paquete oficial) con `WriteTo.Map`
> por `AssociationId`. Se descarta como base porque el cierre del archivo es por evicción
> LRU y no determinista — inaceptable si el archivo debe estar completo y liberado en cuanto
> la asociación termina. Se documenta como fallback si se prefiere cero código de sink.

### 2.4 Configuración

Nueva sección bajo `Diagnostics:File:PerAssociation` (appsettings + push del Hub, igual que
el resto de settings del nodo):

| Clave | Default | Uso |
|---|---|---|
| `Enabled` | `true` | Activa/desactiva la bitácora por asociación |
| `Path` | `logs/associations` | Directorio raíz |
| `MinimumLevel` | `Debug` | Nivel dentro del archivo de asociación (independiente del global) |
| `UseCompactJson` | `false` | Texto plano legible vs. JSON |
| `RetainDays` | `14` | Retención por antigüedad |
| `MaxFilesPerDay` | `5000` | Tope de archivos por día (corta-circuitos ante flood) |
| `MaxFileSizeMb` | `10` | Tope por archivo |
| `IncludeFoDicomInternals` | `true` | Incluir PDU/DIMSE de fo-dicom |

### 2.5 Retención y limpieza

Hosted service de limpieza (mismo patrón que `StudyCleanupService`) que borra por antigüedad
y por tope de archivos/tamaño total del directorio.

### 2.6 PHI

La bitácora pasa por el **mismo `PhiRedactionEnricher`** que el log global (no se abre un
canal sin redactar). El dump de tags de dataset y de PDU queda como **modo diagnóstico
opcional** (§6-C), activable por AE y por tiempo limitado, dado que es el punto donde puede
filtrarse PHI.

### 2.7 Lo que NO cambia

- El log global (`node-YYYYMMDD.log`) sigue **idéntico**: mismo path, formato, enrichers y
  sinks. La bitácora por asociación es **aditiva**.
- No cambia el esquema de `dicom_associations` (salvo el opcional §6-B), ni la lógica de
  aceptación/rechazo, ni el pipeline de estudios.

---

## 3. Análisis de impacto técnico

### 3.1 Componentes afectados / nuevos

| Capa | Archivo / componente | Cambio |
|---|---|---|
| Diagnostics | **`DicomAssociationContext` (nuevo)** | Contexto ambiental `AsyncLocal` por asociación |
| Diagnostics | **`AssociationIdEnricher` (nuevo)** | Enriquecer eventos con `AssociationId`/AEs |
| Diagnostics | **`AssociationLoggerAdapter` (nuevo)** | Decorador `ILogger` para capturar internos de fo-dicom |
| Diagnostics | **`AssociationFileSink` + `AssociationLogWriter` (nuevos)** | Un archivo por asociación, apertura/cierre determinista |
| Diagnostics | [DiagnosticsOptions.cs](src/shared/Dicom.Edge.Diagnostics/Configuration/DiagnosticsOptions.cs) | Nueva sección `File:PerAssociation` + validación |
| Diagnostics | [PlatformDiagnosticsExtensions.cs](src/shared/Dicom.Edge.Diagnostics/Extensions/PlatformDiagnosticsExtensions.cs) | Registrar enricher + sink condicional en `ConfigureSinks` |
| Diagnostics | **`AssociationLogCleanupService` (nuevo)** | Retención por días / nº de archivos / tamaño |
| DicomServer | [CStoreScp.cs](src/edge/Dicom.Edge.Node.DicomServer/CStoreScp.cs) | Generar `AssociationId`, envolver `Logger`, abrir/cerrar bitácora, abrir contexto en cada callback, logs de detalle DIMSE |
| DicomServer | [DicomServerHostedService.cs](src/edge/Dicom.Edge.Node.DicomServer/DicomServerHostedService.cs) | Pasar el escritor de bitácora en `DicomScpDependencies` |
| Node | [DicomInstanceHandler.cs](src/edge/Dicom.Edge.Node/DicomInstanceHandler.cs) | Logs de detalle por instancia (tamaño, ruta, duración) — hereda contexto, sin cambio de firma |
| Node | [DicomAssociationTracker.cs](src/edge/Dicom.Edge.Node/DicomAssociationTracker.cs) | Propagar el `AssociationId` a la sesión (correlación log ↔ BD) |
| Config | `appsettings.json` (Node) | Nueva sección `PerAssociation` |
| Docs | `docs/07-operations/monitoring.md`, `troubleshooting.md` | Cómo leer/ubicar la bitácora en homologación |

### 3.2 Diagrama del flujo

```
  SCU (modalidad)                Edge Node
        │
        ├── A-ASSOCIATE-RQ ──►  CStoreScp (instancia nueva por asociación)
        │                        │ AssociationId = <guid corto>
        │                        │ Logger  ─► AssociationLoggerAdapter ─┐
        │                        │ Abre  assoc-<ts>_<AE>_<id>.log  ◄────┤
        │                        │ Valida AE / catálogo / IP            │
        │        ◄── RJ ─────────┤ (rechazo → se registra y se cierra)  │
        │        ◄── AC ─────────┤ negociación de contextos             │
        │                        │                                      │
        ├── C-STORE-RQ ────────► │ ctx ambiental ► InstanceHandler ──────┤
        │        ◄── RSP ────────┤   (storage + EF + cola)              │  Serilog
        ├── C-FIND-RQ (MWL/QR) ► │ ctx ambiental ► MWL / StudyRoot ──────┤  pipeline
        │        ◄── RSP xN ─────┤                                      │     │
        ├── A-RELEASE-RQ ──────► │ resumen + CompleteAsync()            │     ├─► node-YYYYMMDD.log (global, sin cambios)
        │        ◄── RP ─────────┤ cierra y libera el archivo  ─────────┘     └─► logs/associations/YYYY-MM-DD/<archivo>.log
```

### 3.3 Riesgo técnico principal y su mitigación

fo-dicom invoca los callbacks del SCP desde su propio read-loop; un `AsyncLocal` abierto en
`OnReceiveAssociationRequestAsync` **no fluye** hacia los callbacks posteriores. Por eso el
diseño combina **dos** mecanismos (contexto ambiental abierto *en cada* callback + logger
decorado a nivel de instancia del `DicomService`), en lugar de depender solo de
`LogContext`. Esto ya está considerado en la estimación; es el punto donde una
implementación ingenua falla y produce archivos incompletos.

---

## 4. Supuestos

1. Aplica al **Edge Node** (SCP). Las asociaciones **salientes** del sender (C-STORE SCU a
   PACS) quedan como opcional §6-A.
2. Se conserva el log global tal cual; la bitácora por asociación es un canal adicional en
   disco local del nodo (no se envía al Hub en el alcance base — ver §6-D).
3. Volumen esperado: cientos de asociaciones/día por nodo. Con retención de 14 días y tope
   de tamaño el impacto en disco es de decenas/cientos de MB.
4. La solución **no tiene proyectos de pruebas automatizadas** hoy (no existe ningún
   `*.Tests.csproj` en `EdgeGuard.Platform.slnx`), por lo que la verificación se cotiza como
   **QA de campo** con SCU reales (`storescu`, `findscu`, aborts forzados). Crear la
   infraestructura de pruebas unitarias es el opcional §6-E.

---

## 5. Desglose de esfuerzo

Estimación en **jornadas ideales de desarrollo (JID = 8 h)**. Incluye implementación,
verificación y ajuste de documentación técnica.

**Tarifa aplicada:** **$162.50 MXN/hora** → **$1,300.00 MXN/JID (8 h)**.

| # | Entregable | JID | Horas | Costo (MXN) |
|---|---|---:|---:|---:|
| 1 | `DicomAssociationContext` + `AssociationIdEnricher` + `AssociationLoggerAdapter` | 1.0 | 8 | $1,300.00 |
| 2 | `AssociationFileSink` / writer: archivo por asociación, naming, apertura y cierre determinista, TTL de huérfanas, topes | 2.0 | 16 | $2,600.00 |
| 3 | Integración en `CStoreScp`: ciclo de vida completo (request → accept/reject → DIMSE → release/abort/close) + correlación con `dicom_associations` | 1.5 | 12 | $1,950.00 |
| 4 | Detalle DIMSE por operación (C-STORE con SOP/estatus/duración, C-FIND con llaves y nº de resultados, C-ECHO) + encabezado/resumen de asociación | 1.5 | 12 | $1,950.00 |
| 5 | Opciones `File:PerAssociation` + validación + `appsettings` + soporte de push del Hub | 0.5 | 4 | $650.00 |
| 6 | Servicio de retención/limpieza (días, nº de archivos, tamaño total) | 1.0 | 8 | $1,300.00 |
| 7 | Redacción PHI verificada en el nuevo canal (reuso de `RegexPhiProtector`) | 0.5 | 4 | $650.00 |
| 8 | QA de campo: C-STORE multi-serie, MWL, Q/R, rechazo por AE/IP, abort, 2+ modalidades concurrentes | 1.5 | 12 | $1,950.00 |
| 9 | Documentación operativa (`monitoring.md`, `troubleshooting.md`) | 0.5 | 4 | $650.00 |
| | **Subtotal** | **10.0** | **80** | **$13,000.00** |
| | Gestión / kickoff / buffer de riesgo (~15%) | 1.5 | 12 | $1,950.00 |
| | **Total estimado** | **11.5** | **92** | **$14,950.00** |

> **Total del proyecto base: $14,950.00 MXN** (92 h × $162.50/h), sin IVA.

### 5.1 Alcance mínimo (si se requiere recortar)

Variante **"solo separación por asociación"**, sin detalle DIMSE ampliado, sin limpieza
automática (retención manual) y sin QA extendido: ítems 1, 2, 3, 5 y 7 →
**5.5 JID · 44 h · $7,150.00 MXN**. Entrega el archivo por asociación con lo que hoy ya se
loguea; el detalle fino de cada C-STORE/C-FIND quedaría para una segunda fase.

---

## 6. Opcionales (no incluidos en el total base)

| Opcional | Descripción | JID | Horas | Costo (MXN) |
|---|---|---:|---:|---:|
| A | **Asociaciones salientes**: mismo esquema para el sender (C-STORE SCU y C-ECHO a PACS) en [FoDicomPacsSender.cs](src/edge/Dicom.Edge.Node.Sender/FoDicomPacsSender.cs) | 1.5 | 12 | $1,950.00 |
| B | **Endpoint + UI**: columna `LogFilePath` en `dicom_associations`, endpoint en la Node API y descarga/visor desde la pantalla de asociaciones | 2.5 | 20 | $3,250.00 |
| C | **Modo diagnóstico**: dump de PDU y de tags del dataset, activable por AE y con expiración automática | 1.0 | 8 | $1,300.00 |
| D | **Envío al Hub**: compresión (zip por día/asociación) y subida al Hub para soporte remoto | 1.5 | 12 | $1,950.00 |
| E | **Infraestructura de pruebas**: proyecto de tests + pruebas de integración con SCU en proceso (fo-dicom) para el ciclo de asociación | 2.0 | 16 | $2,600.00 |
| | **Total opcionales (los 5)** | **8.5** | **68** | **$11,050.00** |

---

## 7. Riesgos y consideraciones

- **Propagación de contexto en fo-dicom** (§3.3): es el riesgo principal. Mitigado por
  diseño (doble mecanismo) y verificado en el ítem 8 con asociaciones concurrentes.
- **Concurrencia / handles de archivo:** con `MaxClients` alto puede haber decenas de
  archivos abiertos. Mitigado con tope de sinks vivos, escritura buffered con flush al
  cierre y cierre determinista en release/abort.
- **I/O y rendimiento:** escritura adicional por asociación. Mitigado usando el mismo patrón
  `WriteTo.Async` del log global; el impacto esperado es marginal frente a la escritura de
  los propios `.dcm`.
- **Crecimiento en disco / flood de asociaciones:** un SCU en bucle puede generar miles de
  archivos. Mitigado con `MaxFilesPerDay`, `MaxFileSizeMb` y el servicio de limpieza.
- **PHI:** el nuevo canal reutiliza la redacción existente; los dumps crudos quedan
  deliberadamente fuera del alcance base (opcional C, con activación temporal).
- **Homologación:** este entregable está pensado para acortar el ciclo de homologación con
  modalidades nuevas — el archivo por asociación es lo que se adjunta al proveedor cuando la
  modalidad no logra asociarse o envía objetos que el nodo rechaza.

---

## 8. Entregables

1. Bitácora independiente por asociación DICOM entrante, desde `A-ASSOCIATE-RQ` hasta
   release/abort/cierre, incluyendo asociaciones **rechazadas**.
2. Detalle por operación DIMSE (C-STORE, C-FIND MWL, C-FIND Study Root, C-ECHO) con estatus
   y duración, más resumen de cierre.
3. Log global existente intacto (sin regresión).
4. Configuración por `appsettings` y por push del Hub, con activación/desactivación.
5. Retención y limpieza automática de las bitácoras.
6. Correlación `AssociationId` entre la bitácora, el log global y la tabla `dicom_associations`.
7. Documentación operativa de uso en soporte y homologación.

---

## 9. Cronograma sugerido

| Semana | Actividades |
|---|---|
| 1 | Contexto/enricher/adapter, sink de archivo por asociación, integración en `CStoreScp` |
| 2 | Detalle DIMSE + resumen, opciones y limpieza, PHI, QA de campo y documentación |

> Estimado: **≈ 11.5 JID (~2 semanas)** con 1 desarrollador backend.

---

_Cotización sujeta a validación en kickoff del formato de nombre de archivo, política de
retención y nivel de detalle (texto legible vs. JSON). Montos calculados a tarifa de
**$162.50 MXN/hora**, no incluyen IVA._
