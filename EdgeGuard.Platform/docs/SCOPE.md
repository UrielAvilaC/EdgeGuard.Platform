# EdgeGuard.Platform — Alcance Actual

> Documento generado para contrastar el estado actual de la implementación
> contra los objetivos definidos en el README.md del repositorio.

---

## Estructura del Monorepo (24 proyectos)

```
EdgeGuard.Platform/
├── backend/
│   ├── Dicom.Edge.Hub.Api              ← API REST (ASP.NET Core)
│   ├── Dicom.Edge.Hub.Application      ← Lógica de aplicación, HL7 pipeline
│   ├── Dicom.Edge.Hub.Domain           ← Dominio DDD (agregados, entidades, value objects)
│   ├── Dicom.Edge.Hub.Infrastructure   ← EF Core repos, hosted services, TCP listener
│   ├── Dicom.Edge.Hub.Persistence      ← Configuraciones y migraciones de BD
│   └── Dicom.Edge.Hub.Diagnostics      ← Diagnósticos enterprise del Hub
│
├── edge/
│   ├── Dicom.Edge.Node                 ← Worker Service (host del nodo)
│   ├── Dicom.Edge.Node.Api             ← API REST del nodo
│   ├── Dicom.Edge.Node.Configuration   ← Configuración del nodo
│   ├── Dicom.Edge.Node.DicomServer     ← Servidor DICOM (C-STORE SCP, MWL)
│   ├── Dicom.Edge.Node.Diagnostics     ← Diagnósticos enterprise del nodo (wrapper)
│   ├── Dicom.Edge.Node.Persistence     ← Persistencia local (SQLite)
│   ├── Dicom.Edge.Node.Processing      ← Pipeline de procesamiento
│   ├── Dicom.Edge.Node.Queue           ← Cola de estudios
│   ├── Dicom.Edge.Node.Router          ← Enrutamiento de estudios
│   ├── Dicom.Edge.Node.Sender          ← Envío al PACS destino
│   ├── Dicom.Edge.Node.Storage         ← Almacenamiento temporal
│   └── Dicom.Edge.Node.Worklist        ← Modality Worklist (MWL)
│
└── shared/
    ├── Dicom.Edge.Abstractions         ← Interfaces (IRepository, IAuditLogger, IMetricsCollector)
    ├── Dicom.Edge.Common               ← Paginación, utilidades compartidas
    ├── Dicom.Edge.Contracts            ← DTOs, rutas de API, contratos
    ├── Dicom.Edge.Diagnostics          ← Diagnósticos enterprise compartidos (Serilog, OTel, PHI)
    ├── Dicom.Edge.Models               ← Modelos de dominio compartidos
    └── Dicom.Edge.Security             ← Seguridad compartida
```

---

## Objetivos del README vs Estado Actual

| # | Objetivo (README) | Estado | Notas |
|---|-------------------|--------|-------|
| 1 | Recibir estudios desde modalidades (CT, MR, CR, US, etc.) | ✅ Implementado | `Dicom.Edge.Node.DicomServer` — C-STORE SCP |
| 2 | Proveer Modality Worklist (MWL) a equipos médicos | ✅ Implementado | `Dicom.Edge.Node.Worklist` |
| 3 | Almacenamiento temporal de estudios en nodos Edge | ✅ Implementado | `Dicom.Edge.Node.Storage` + `Node.Persistence` (SQLite) |
| 4 | Enviar estudios al PACS destino de forma segura | ✅ Implementado | `Dicom.Edge.Node.Sender` + `Node.Router` |
| 5 | Herramientas administrativas (interfaz web) | ❌ Pendiente | No existe frontend (SPA / AdminUI) |
| 6 | Operar en entornos con conectividad intermitente | ✅ Implementado | `Dicom.Edge.Node.Queue` con reintentos automáticos |

---

## Funcionalidades Implementadas

### Edge Node (Nodo Local)
- **C-STORE SCP**: Servidor DICOM que recibe imágenes desde modalidades
- **Modality Worklist (MWL)**: Servidor C-FIND que provee listas de trabajo
- **Cola de procesamiento**: Pipeline asíncrono con reintentos
- **Almacenamiento temporal**: Persistencia local en SQLite
- **Envío al PACS**: Los nodos envían estudios **directamente al PACS destino** (no al Hub)
- **Enrutamiento**: Lógica de routing configurable
- **API REST**: Endpoints de control y estado del nodo
- **Diagnósticos enterprise**: Serilog + OpenTelemetry (thin wrapper sobre shared)

### Hub Central (Backend)
- **Dominio DDD** con agregados: Nodes, Studies, Patients, PacsServers, Routing, Configuration
- **Pipeline HL7 enterprise**: TCP Listener → Validación → Persistencia → ACK → Enrutamiento → Cola → Despacho
- **10 Controllers REST**: Nodes, Studies, Patients, PacsServers, SystemSettings, RoutingRules, Hl7Status, QueueMonitoring, Hub, Edge
- **Hosted Services**: `Hl7ListenerHostedService`, `MessageDispatchHostedService`, `NodeHealthEvaluationHostedService`, `StudyCleanupEvaluationHostedService`
- **Repositorios EF Core**: PostgreSQL con async/await y self-saving `UpdateAsync`
- **Monitoreo HL7**: Estadísticas en tiempo real, conteo por tipo/estado, métricas de rendimiento
- **Despacho HTTP**: `NodeHttpDispatcher` para envío de worklist y comandos a nodos
- **Diagnósticos enterprise**: Serilog + OpenTelemetry + HL7 health check

### Diagnósticos Enterprise (3 capas)
- **Shared (`Dicom.Edge.Diagnostics`)**: Logging estructurado (Serilog), trazas distribuidas (OpenTelemetry), redacción de PHI, correlación de requests, auditoría, health checks, middleware
- **Hub (`Dicom.Edge.Hub.Diagnostics`)**: Scopes HL7, constantes de pipeline, health check del listener TCP
- **Node (`Dicom.Edge.Node.Diagnostics`)**: Métricas DICOM, health check de conectividad PACS

### Shared
- **Abstractions**: Interfaces genéricas (`IRepository<T>`, `IAuditLogger`, `IMetricsCollector`, `IStorageProvider`, `IEdgeQueue<T>`)
- **Contracts**: DTOs y rutas de API (`HubApiRoutes`, `Hl7WorklistPushDto`)
- **Common**: Paginación (`PagedResult<T>`, `PaginationRequest`)
- **Models**: Modelos de dominio compartidos (`DicomStudy`)
- **Security**: Componentes de seguridad compartidos

---

## Funcionalidades Pendientes

| Componente | Descripción | Prioridad |
|------------|-------------|-----------|
| **Frontend SPA** (`EdgeGuard.Spa`) | Visualización de estudios, monitoreo de nodos, configuración | Alta |
| **Frontend Admin** (`EdgeGuard.AdminUi`) | Administración del sistema, configuración avanzada | Alta |
| **Autenticación / Autorización** | JWT / OAuth en APIs Hub y Node | Alta |
| **Migraciones EF Core** | Scripts de migración para PostgreSQL (Hub) | Media |
| **Tests unitarios e integración** | Cobertura de pruebas para todos los componentes | Media |
| **CI/CD Pipeline** | Build, test, deploy automatizado | Media |
| **Documentación API** | Swagger/OpenAPI con descripciones detalladas | Baja |
| **Monitoreo centralizado** | Dashboard Grafana/Prometheus con métricas OTel | Baja |

---

## Correcciones al README

El README original indicaba erróneamente que los nodos Edge envían estudios al Hub central.
**Corrección aplicada**: los nodos envían estudios **directamente al PACS destino**.
El Hub central es responsable de la **orquestación, configuración y monitoreo** de los nodos,
no de recibir estudios DICOM.

---

## Stack Tecnológico Actual

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Runtime | .NET | 10.0 |
| Lenguaje | C# | 14.0 |
| Hub API | ASP.NET Core | 10.0 |
| Edge Node | Worker Service | 10.0 |
| Hub DB | PostgreSQL (Npgsql) | 10.0.1 |
| Node DB | SQLite (EF Core) | 10.0.5 |
| Logging | Serilog | 10.0.0 |
| Trazas | OpenTelemetry | 1.15.0 |
| HL7 | TCP/MLLP custom | — |
| DICOM | fo-dicom (Node) | — |
