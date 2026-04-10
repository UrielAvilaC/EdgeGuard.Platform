# EdgeGuard.Platform

## Plataforma distribuida para integración de imágenes médicas

**EdgeGuard.Platform** es una plataforma enterprise diseñada para la recepción,
gestión y distribución de estudios médicos **DICOM** e integración **HL7 v2.5**
en entornos clínicos hospitalarios.

La solución sigue una arquitectura **Edge + Hub**, donde nodos locales
instalados en hospitales reciben estudios desde modalidades médicas y
los envían directamente al PACS destino. El Hub central orquesta la
configuración, monitoreo, integración HL7 y administración de los nodos.

La interfaz web es un **único proyecto Angular** que unifica el
**Dashboard operativo** y el **Panel de administración** mediante
módulos lazy-loaded, guardias de autorización y layouts diferenciados.

------------------------------------------------------------------------

# Objetivos del proyecto

-   Recibir estudios médicos desde modalidades (CT, MR, CR, US, etc.)
-   Proveer **Modality Worklist (MWL)** a equipos médicos
-   Almacenar temporalmente estudios en nodos Edge
-   Enviar estudios directamente al PACS destino de forma segura
-   Proporcionar herramientas administrativas mediante una interfaz web
-   Operar correctamente en entornos con conectividad intermitente

------------------------------------------------------------------------

# Arquitectura general

```
                         ┌─────────────────────────────────────┐
                         │         EdgeGuard.UI (Angular)      │
                         │                                     │
                         │  ┌─────────────┐ ┌───────────────┐  │
                         │  │  Dashboard  │ │   Admin Panel  │  │
                         │  │  (Operator) │ │ (Administrador)│  │
                         │  └──────┬──────┘ └───────┬────────┘  │
                         │         └────────┬───────┘           │
                         └──────────────────┼───────────────────┘
                                            │ HTTP/REST
                                            ▼
                         ┌─────────────────────────────────────┐
                         │       EdgeGuard Hub.Api (.NET 10)   │
                         │  ┌─────────┐ ┌────────┐ ┌────────┐ │
                         │  │Operator │ │ Admin  │ │  Edge  │ │
                         │  │Endpoints│ │Endpts  │ │Endpts  │ │
                         │  └─────────┘ └────────┘ └────────┘ │
                         └──────────────────┬──────────────────┘
                               ┌────────────┼────────────┐
                               │            │            │
                               ▼            ▼            ▼
                         ┌──────────┐ ┌──────────┐ ┌──────────┐
                         │ Edge     │ │ Edge     │ │ Edge     │
                         │ Node A   │ │ Node B   │ │ Node C   │
                         │ (DICOM)  │ │ (DICOM)  │ │ (DICOM)  │
                         └──────────┘ └──────────┘ └──────────┘
                              │            │            │
                              ▼            ▼            ▼
                         Modalidades (CT, MR, CR, US, etc.)
```

> **Decisión arquitectónica:** Un solo `Hub.Api` sirve tanto al Dashboard
> como al Admin Panel. La separación es lógica mediante políticas de
> autorización (`Operator` / `Admin`), no mediante APIs separadas.

------------------------------------------------------------------------

# Componentes principales

La plataforma está organizada como un **monorepositorio** que contiene
múltiples aplicaciones.

```
EdgeGuard.Platform
│
├── src/
│   ├── backend/                          ← Hub (servidor central)
│   │   ├── Dicom.Edge.Hub.Api            ← API REST (.NET 10)
│   │   ├── Dicom.Edge.Hub.Application    ← Lógica de negocio
│   │   ├── Dicom.Edge.Hub.Domain         ← Entidades y agregados
│   │   ├── Dicom.Edge.Hub.Infrastructure ← TCP Listener, Dispatch, Jobs
│   │   ├── Dicom.Edge.Hub.Persistence    ← EF Core + PostgreSQL
│   │   └── Dicom.Edge.Hub.Diagnostics    ← Observabilidad Hub
│   │
│   ├── edge/                             ← Edge Node (gateway DICOM)
│   │   ├── Dicom.Edge.Node               ← Worker Service host
│   │   ├── Dicom.Edge.Node.Api           ← API interna del nodo
│   │   ├── Dicom.Edge.Node.DicomServer   ← C-STORE SCP + MWL
│   │   ├── Dicom.Edge.Node.Processing    ← Pipeline de estudios
│   │   ├── Dicom.Edge.Node.Sender        ← Envío al PACS
│   │   ├── Dicom.Edge.Node.Router        ← Enrutamiento de estudios
│   │   ├── Dicom.Edge.Node.Worklist      ← Modality Worklist
│   │   ├── Dicom.Edge.Node.Queue         ← Cola de procesamiento
│   │   ├── Dicom.Edge.Node.Configuration ← Sincronización Hub↔Node
│   │   ├── Dicom.Edge.Node.Persistence   ← EF Core + SQLite/Postgres
│   │   └── Dicom.Edge.Node.Diagnostics   ← Observabilidad Node
│   │
│   ├── frontend/                         ← Interfaz web unificada
│   │   └── EdgeGuard.UI                  ← Angular (Dashboard + Admin)
│   │
│   └── shared/                           ← Componentes compartidos
│       ├── Dicom.Edge.Abstractions       ← Interfaces base
│       ├── Dicom.Edge.Contracts          ← DTOs y contratos de API
│       ├── Dicom.Edge.Common             ← Utilidades compartidas
│       ├── Dicom.Edge.Models             ← Modelos de dominio compartidos
│       └── Dicom.Edge.Diagnostics        ← Observabilidad compartida
│
├── docs/                                 ← Documentación técnica
└── scripts/                              ← Scripts de despliegue
```

------------------------------------------------------------------------

# Descripción de componentes

## backend — Hub API (.NET 10)

Servidor central que expone una **única API REST** para el frontend y
gestiona la comunicación con los nodos Edge.

Responsabilidades:

-   Recepción HL7 v2.5 sobre MLLP (TCP Listener)
-   Validación enterprise y enrutamiento de mensajes HL7
-   Gestión de estudios, pacientes y nodos
-   Dispatch de worklist a nodos Edge (HTTP)
-   Configuración centralizada y sincronización Hub → Node
-   Auditoría y trazabilidad completa
-   Notificación de resultados vía WhatsApp
-   Políticas de retención de datos

------------------------------------------------------------------------

## frontend — EdgeGuard.UI (Angular)

**Proyecto Angular único** que unifica el Dashboard operativo y el Panel
de administración en una sola aplicación con módulos lazy-loaded.

> **¿Por qué un solo proyecto?** Comparten modelos, DTOs, servicios HTTP,
> interceptores de autenticación y pipeline de CI/CD. La separación es
> lógica (rutas y guardias), no física.

### Arquitectura del frontend

```
EdgeGuard.UI/
│
├── src/
│   ├── app/
│   │   ├── core/                         ← Singleton services (auth, API, interceptors)
│   │   │   ├── auth/                     ← AuthService, AuthGuard, AdminGuard
│   │   │   ├── api/                      ← ApiService, interceptors HTTP
│   │   │   └── layout/                   ← Resolvers, navigation state
│   │   │
│   │   ├── shared/                       ← Reutilizables (sin estado)
│   │   │   ├── models/                   ← Interfaces TypeScript (DTOs)
│   │   │   ├── components/               ← DataTable, StatusBadge, Confirm Dialog
│   │   │   ├── pipes/                    ← DateFormat, Hl7Status, FileSize
│   │   │   └── directives/               ← HasRole, AutoFocus, Tooltip
│   │   │
│   │   ├── layouts/                      ← Layouts diferenciados
│   │   │   ├── spa-layout/               ← Sidebar operativa + header
│   │   │   └── admin-layout/             ← Sidebar administrativa + header
│   │   │
│   │   └── features/                     ← Módulos lazy-loaded
│   │       ├── dashboard/                ← Dashboard operativo
│   │       ├── studies/                  ← Gestión de estudios
│   │       ├── patients/                 ← Gestión de pacientes
│   │       ├── hl7-monitor/              ← Monitoreo HL7 en tiempo real
│   │       └── admin/                    ← Panel de administración
│   │           ├── admin-dashboard/      ← Resumen administrativo
│   │           ├── nodes-management/     ← Gestión de nodos Edge
│   │           ├── pacs-management/      ← Servidores PACS
│   │           ├── routing-rules/        ← Reglas de enrutamiento HL7
│   │           ├── node-configuration/   ← Perfiles de configuración
│   │           ├── system-settings/      ← Configuración del sistema
│   │           ├── audit-log/            ← Log de auditoría
│   │           └── data-retention/       ← Políticas de retención
│   │
│   ├── assets/
│   └── environments/
│
├── angular.json
├── package.json
└── tsconfig.json
```

### Módulos del frontend — Resumen

| # | Módulo | Área | Páginas | Controlador Backend | Guard |
|---|--------|------|:-------:|---------------------|-------|
| 1 | `dashboard` | Operador | 1 | `HubController` | `AuthGuard` |
| 2 | `studies` | Operador | 2 | `StudiesController` | `AuthGuard` |
| 3 | `patients` | Operador | 2 | `PatientsController` | `AuthGuard` |
| 4 | `hl7-monitor` | Operador | 3 | `Hl7StatusController`, `QueueMonitoringController` | `AuthGuard` |
| 5 | `admin-dashboard` | Admin | 1 | `HubController`, `NodesController` | `AdminGuard` |
| 6 | `nodes-management` | Admin | 2 | `NodesController` | `AdminGuard` |
| 7 | `pacs-management` | Admin | 2 | `PacsServersController` | `AdminGuard` |
| 8 | `routing-rules` | Admin | 2 | `RoutingRulesController` | `AdminGuard` |
| 9 | `node-configuration` | Admin | 2 | `NodeConfigurationController` | `AdminGuard` |
| 10 | `system-settings` | Admin | 1 | `SystemSettingsController` | `AdminGuard` |
| 11 | `audit-log` | Admin | 1 | `AuditController` | `AdminGuard` |
| 12 | `data-retention` | Admin | 1 | `SystemSettingsController` | `AdminGuard` |
| 13 | `auth` | Público | 1 | — | — |

> **Total: 13 módulos · 21 páginas · 100% lazy-loaded**

### Menú de navegación — Dashboard (Operador)

```
┌────────────────────────┐
│  🏥 EdgeGuard          │
│                        │
│  📊 Dashboard          │  ← /dashboard
│  📋 Estudios           │  ← /studies
│  👥 Pacientes          │  ← /patients
│  📡 Monitor HL7        │  ← /hl7/status
│     ├─ Estado          │  ← /hl7/status
│     ├─ Cola            │  ← /hl7/queue
│     └─ Mensajes        │  ← /hl7/messages
│                        │
│  ──────────────────    │
│  ⚙️ Administración →   │  ← /admin (solo con rol Admin)
│  🚪 Cerrar sesión      │
└────────────────────────┘
```

### Menú de navegación — Admin Panel (Administrador)

```
┌────────────────────────┐
│  🛡️ EdgeGuard Admin    │
│                        │
│  📊 Resumen            │  ← /admin/dashboard
│  🖥️ Nodos Edge         │  ← /admin/nodes
│  🏥 Servidores PACS    │  ← /admin/pacs
│  🔀 Reglas de Routing  │  ← /admin/routing-rules
│  📦 Configuración Node │  ← /admin/node-config
│  ⚙️ Sistema            │  ← /admin/settings
│  📝 Auditoría          │  ← /admin/audit
│  🗑️ Retención de Datos │  ← /admin/data-retention
│                        │
│  ──────────────────    │
│  ← Volver al Dashboard │  ← /dashboard
│  🚪 Cerrar sesión      │
└────────────────────────┘
```

### Mapa de rutas

| Ruta | Módulo | Layout | Guard |
|------|--------|--------|-------|
| `/login` | `auth` | — | — |
| `/dashboard` | `dashboard` | `SpaLayout` | `AuthGuard` |
| `/studies` | `studies` | `SpaLayout` | `AuthGuard` |
| `/studies/:id` | `studies` | `SpaLayout` | `AuthGuard` |
| `/patients` | `patients` | `SpaLayout` | `AuthGuard` |
| `/patients/:id` | `patients` | `SpaLayout` | `AuthGuard` |
| `/hl7/status` | `hl7-monitor` | `SpaLayout` | `AuthGuard` |
| `/hl7/queue` | `hl7-monitor` | `SpaLayout` | `AuthGuard` |
| `/hl7/messages/:id` | `hl7-monitor` | `SpaLayout` | `AuthGuard` |
| `/admin/dashboard` | `admin-dashboard` | `AdminLayout` | `AdminGuard` |
| `/admin/nodes` | `nodes-management` | `AdminLayout` | `AdminGuard` |
| `/admin/nodes/:id` | `nodes-management` | `AdminLayout` | `AdminGuard` |
| `/admin/pacs` | `pacs-management` | `AdminLayout` | `AdminGuard` |
| `/admin/pacs/:id` | `pacs-management` | `AdminLayout` | `AdminGuard` |
| `/admin/routing-rules` | `routing-rules` | `AdminLayout` | `AdminGuard` |
| `/admin/routing-rules/:id` | `routing-rules` | `AdminLayout` | `AdminGuard` |
| `/admin/node-config` | `node-configuration` | `AdminLayout` | `AdminGuard` |
| `/admin/node-config/:id` | `node-configuration` | `AdminLayout` | `AdminGuard` |
| `/admin/settings` | `system-settings` | `AdminLayout` | `AdminGuard` |
| `/admin/audit` | `audit-log` | `AdminLayout` | `AdminGuard` |
| `/admin/data-retention` | `data-retention` | `AdminLayout` | `AdminGuard` |

### Autorización y guardias

| Guard | Protege | Validación |
|-------|---------|------------|
| `AuthGuard` | Todas las rutas excepto `/login` | Token JWT válido y no expirado |
| `AdminGuard` | Rutas `/admin/**` | Token JWT con rol `Admin` |

> Ambos guards se implementan como `CanActivate` funcionales.
> `AdminGuard` extiende la validación de `AuthGuard` agregando la
> verificación del claim de rol.

------------------------------------------------------------------------

## edge — Edge Node (Gateway DICOM)

Nodo instalado en hospitales o clínicas que actúa como **gateway
DICOM**.

Responsabilidades:

-   Servidor **DICOM C-STORE SCP** para recibir imágenes
-   Servidor **DICOM Modality Worklist (MWL)** con C-FIND
-   Recepción de worklist push desde el Hub (HTTP)
-   Almacenamiento temporal de estudios
-   Cola de procesamiento con reintentos
-   Enrutamiento y envío de estudios al PACS destino
-   Sincronización de configuración con el Hub
-   Health checks y heartbeat periódico

------------------------------------------------------------------------

## shared — Componentes compartidos

Bibliotecas compartidas entre Hub y Edge Node.

Incluye:

-   **Abstractions**: Interfaces base (repositorios, servicios, métricas)
-   **Contracts**: DTOs, contratos de API, configuración compartida
-   **Common**: Paginación, utilidades
-   **Models**: Modelos de dominio compartidos
-   **Diagnostics**: Serilog, OpenTelemetry, PHI redaction, health checks

------------------------------------------------------------------------

# Flujo de datos

    Modalidades médicas
          │
          │  C-FIND (Worklist)
          ▼
    EdgeGuard EdgeNode
          │
          │  C-STORE
          ▼
    Almacenamiento local
          │
          ▼
    Cola de estudios
          │
          ▼
    Envío al PACS destino
          │
          ▼
    Notificación al Hub central
          │
          ▼
    Entrega de resultado vía WhatsApp

------------------------------------------------------------------------

# Tecnologías utilizadas

### Backend (Hub API)

| Tecnología | Versión | Uso |
|------------|---------|-----|
| .NET | 10 | Runtime y framework |
| ASP.NET Core | 10 | API REST |
| Entity Framework Core | 10 | ORM |
| PostgreSQL | 16+ | Base de datos Hub |
| Serilog | 4.x | Logging estructurado |
| OpenTelemetry | 1.x | Métricas y trazas |

### Edge Node

| Tecnología | Versión | Uso |
|------------|---------|-----|
| .NET Worker Service | 10 | Host del nodo |
| fo-dicom | 5.x | Servidor DICOM (C-STORE, MWL) |
| SQLite / PostgreSQL | — | Persistencia local |
| Serilog + OpenTelemetry | — | Observabilidad |

### Frontend

| Tecnología | Versión | Uso |
|------------|---------|-----|
| Angular | 19+ | Framework SPA |
| TypeScript | 5.x | Lenguaje principal |
| Angular Material / PrimeNG | — | Componentes UI |
| RxJS | 7.x | Programación reactiva |
| Angular Router | — | Routing con lazy loading |
| JWT | — | Autenticación con guards |

### Protocolos e integración

| Protocolo | Estándar | Uso |
|-----------|----------|-----|
| HL7 v2.5 | MLLP/TCP | Integración HIS/RIS |
| DICOM | C-STORE, C-FIND | Recepción de imágenes y MWL |
| HTTP/REST | JSON | Comunicación Hub ↔ Node ↔ UI |

------------------------------------------------------------------------

# Características principales

| Área | Capacidad | Descripción |
|------|-----------|-------------|
| Arquitectura | **Edge + Hub** | Nodos locales autónomos con Hub central de orquestación |
| DICOM | **C-STORE SCP** | Recepción de imágenes desde modalidades |
| DICOM | **Modality Worklist (MWL)** | C-FIND para que las modalidades consulten estudios programados |
| HL7 | **Listener MLLP** | Recepción de mensajes HL7 v2.5 sobre TCP |
| HL7 | **Validación Enterprise** | Validación estructural y semántica por tipo (ADT, ORM, ORU) |
| HL7 | **Routing Engine** | Motor de reglas con prioridad, fallback automático |
| HL7 | **Dispatch Confiable** | Reintentos automáticos con backoff configurable |
| Procesamiento | **Colas asíncronas** | Pipeline de procesamiento con reintentos |
| Resiliencia | **Offline-first** | Operación autónoma del Edge Node sin conectividad |
| Sincronización | **Hub ↔ Node** | Configuración centralizada con push automático |
| Notificaciones | **WhatsApp** | Entrega automática y manual de resultados |
| Observabilidad | **Enterprise** | Serilog + OpenTelemetry + PHI redaction |
| Frontend | **Angular Unificado** | Dashboard + Admin en un solo proyecto con lazy loading |
| Seguridad | **RBAC** | Roles Operator/Admin con JWT y guardias de ruta |

------------------------------------------------------------------------

# Entrega de resultados vía WhatsApp

La plataforma permite notificar al paciente o médico referente cuando un
estudio ha sido procesado y almacenado exitosamente en el PACS. La
entrega del resultado se realiza mediante un mensaje de **WhatsApp** que
incluye el enlace directo al visor PACS.

> **Nota:** La notificación solo se ejecuta si el paciente tiene un
> número de teléfono registrado. Si no existe teléfono en el registro
> del paciente, el flujo de WhatsApp se omite silenciosamente.

## Configuración dinámica

La entrega de resultados se controla mediante **claves de configuración
del sistema** (`system_settings`), lo que permite habilitar o
deshabilitar cada comportamiento en tiempo de ejecución sin necesidad de
reiniciar la aplicación.

| Clave                                         | Tipo    | Descripción                                                       |
|-----------------------------------------------|---------|-------------------------------------------------------------------|
| `whatsapp.enabled`                            | bool    | Habilita o deshabilita globalmente el servicio de WhatsApp        |
| `whatsapp.auto_send_on_oru`                   | bool    | Envío automático al recibir ACK tras un ORU^R01                   |
| `whatsapp.require_pacs_link`                  | bool    | Si `true`, solo envía si el enlace al visor PACS está disponible  |
| `whatsapp.api_base_url`                       | string  | URL base de la API de mensajería WhatsApp                         |
| `whatsapp.default_message_template`           | string  | Identificador de la plantilla de mensaje por defecto              |
| `whatsapp.retry_max_attempts`                 | int     | Número máximo de reintentos en caso de fallo de envío             |
| `whatsapp.retry_delay_seconds`                | int     | Segundos de espera entre reintentos                               |

## Flujo automático (ORU → PACS → WhatsApp)

    Hub HL7 Listener
          │
          │  Recibe mensaje ORU^R01
          ▼
    Pipeline HL7 (validación + enrutamiento)
          │
          │  Encolado y despacho
          ▼
    Edge Node destino
          │
          │  C-STORE al PACS
          ▼
    PACS destino
          │
          │  ACK de confirmación
          ▼
    Edge Node → notifica al Hub
          │
          ▼
    Hub evalúa condiciones:
      ├─ whatsapp.enabled = true?
      ├─ whatsapp.auto_send_on_oru = true?
      ├─ ¿Paciente tiene teléfono?
      └─ ¿Enlace PACS disponible? (si require_pacs_link = true)
          │
          │  Si todas las condiciones se cumplen
          ▼
    Servicio de notificación WhatsApp
          │
          │  Genera enlace al visor PACS
          │  Envía mensaje al paciente / referente
          ▼
    WhatsApp (API de mensajería)

1.  El Hub recibe un mensaje **ORU^R01** (resultado no solicitado) a
    través del listener HL7.
2.  El mensaje es validado, enrutado y encolado hacia el nodo Edge
    correspondiente.
3.  El nodo Edge envía el estudio al **PACS destino** mediante
    C-STORE.
4.  Al recibir el **ACK** de confirmación del PACS, el nodo notifica al
    Hub que el estudio fue almacenado exitosamente.
5.  El Hub evalúa las condiciones de envío:
    -   `whatsapp.enabled` y `whatsapp.auto_send_on_oru` deben ser
        `true`.
    -   El paciente debe tener un **número de teléfono** registrado.
    -   Si `whatsapp.require_pacs_link` es `true`, el enlace al visor
        PACS debe estar disponible.
6.  Si todas las condiciones se cumplen, genera el enlace al visor PACS
    y envía la notificación vía **WhatsApp**.

> Si alguna condición no se cumple, el flujo se omite y se registra un
> log informativo. No se genera error.

## Entrega manual (desde la interfaz)

Desde la interfaz de administración, un operador puede iniciar la
entrega de resultados de forma manual:

-   **Si el enlace al visor PACS está disponible**: se entrega
    directamente al paciente vía WhatsApp.
-   **Si el enlace no está disponible**: el sistema solicita al operador
    que proporcione o confirme el enlace antes de enviar la
    notificación.
-   **Si el paciente no tiene teléfono**: el operador puede ingresar el
    número manualmente antes de enviar.

Este modo permite manejar casos donde el estudio fue almacenado
previamente, donde el enlace PACS requiere validación manual, o donde el
paciente no tiene teléfono registrado en el sistema.

## Datos utilizados para la notificación

| Campo                  | Obligatorio | Origen                                         |
|------------------------|-------------|-------------------------------------------------|
| Número de teléfono     | No*         | Datos del paciente (HL7 / registro) o manual    |
| Nombre del paciente    | Sí          | Mensaje ORU / registro del estudio              |
| Enlace al visor PACS   | Configurable| PACS destino o ingreso manual                   |
| Descripción del estudio| Sí          | Metadata del estudio DICOM                      |

\* Si no existe teléfono, el flujo automático se omite. En modo manual
el operador puede ingresarlo.

------------------------------------------------------------------------

# Estado del proyecto

En desarrollo activo.

### Backend (Hub + Edge Node) — ✅ Implementado

1.  ✅ Servidor **DICOM C-STORE SCP**
2.  ✅ Servidor **DICOM Modality Worklist (MWL)** con C-FIND
3.  ✅ Pipeline de ingestión y procesamiento de estudios
4.  ✅ Envío de estudios al PACS destino con reintentos
5.  ✅ Pipeline HL7 completo (recepción MLLP, validación, enrutamiento, despacho)
6.  ✅ Diagnósticos enterprise (Serilog, OpenTelemetry, PHI redaction)
7.  ✅ Entrega de resultados vía **WhatsApp** (automática y manual)
8.  ✅ Sincronización de configuración Hub → Node
9.  ✅ Auditoría y retención de datos
10. ✅ API REST completa para Dashboard y Admin

### Frontend (Angular) — 🔄 En desarrollo

1.  🔄 Proyecto Angular con estructura de módulos lazy-loaded
2.  ⬜ Core: Autenticación JWT, interceptores HTTP, API service
3.  ⬜ Dashboard operativo (estudios, pacientes, HL7 monitor)
4.  ⬜ Panel de administración (nodos, PACS, routing, configuración)
5.  ⬜ Componentes compartidos (tablas, badges, diálogos)
6.  ⬜ Layouts diferenciados (SPA + Admin)

------------------------------------------------------------------------

# Seguridad

La plataforma está diseñada considerando:

-   interoperabilidad con estándares médicos
-   integridad de estudios clínicos
-   operación segura en redes hospitalarias
-   aislamiento de nodos Edge

------------------------------------------------------------------------

# Licencia

Software propietario.

Distribución, copia o modificación no autorizada está prohibida.

------------------------------------------------------------------------

# Autor

Equipo de ingeniería de **EdgeGuard Platform**
