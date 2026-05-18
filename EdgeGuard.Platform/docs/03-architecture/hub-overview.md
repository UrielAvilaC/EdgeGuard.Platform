# Hub Central — Component Overview

The EdgeGuard Hub is the central coordination point for the platform. It receives HL7 messages from hospital information systems, manages studies and patients, pushes configuration to distributed Edge Nodes, and provides the API and real-time dashboard surface consumed by the web SPA.

---

## Project Relationship Diagram

```mermaid
flowchart TB
    subgraph Hub["EdgeGuard Hub"]
        API["Dicom.Edge.Hub.Api<br/>REST Controllers · SignalR · Middleware<br/>Rate Limiting · Swagger"]
        APP["Dicom.Edge.Hub.Application<br/>CQRS Handlers · Validators<br/>Domain Event Handlers · Mapping"]
        DOM["Dicom.Edge.Hub.Domain<br/>Aggregates · Entities<br/>Value Objects · Repository Interfaces"]
        PERS[("Dicom.Edge.Hub.Persistence<br/>AppDbContext · EF Core<br/>PostgreSQL · Migrations")]
        INFRA["Dicom.Edge.Hub.Infrastructure<br/>HL7 TCP Listener · Node HTTP Push Client<br/>External Services"]
        DIAG["Dicom.Edge.Hub.Diagnostics<br/>Serilog Bootstrap · OpenTelemetry<br/>Health Checks · PHI Redact"]
    end

    API -- "MediatR commands/queries" --> APP
    APP --> DOM
    APP --> PERS
    PERS -. "implements repository interfaces" .-> DOM
    INFRA --> APP
    INFRA --> DOM
    API --> DIAG
    INFRA --> DIAG

    classDef hub fill:#bbdefb,stroke:#1976d2,color:#0d47a1
    classDef db fill:#f8bbd0,stroke:#c2185b,color:#880e4f
    class API,APP,DOM,INFRA,DIAG hub
    class PERS db
```

---

## Project Reference Table

| Project | Responsibility | Key Classes / Concepts |
|---|---|---|
| `Dicom.Edge.Hub.Domain` | Core business rules, aggregates, domain events, repository contracts | `Study`, `Patient`, `NodeDicomRoutingRule`, `StudyStatus`, `IStudyRepository`, `IPatientRepository` |
| `Dicom.Edge.Hub.Application` | Orchestrates use cases via CQRS (MediatR), validates inputs, maps domain → DTO | `ReceiveStudyCommandHandler`, `SyncPatientCommandHandler`, `GetStudiesQueryHandler`, `StudyMappingProfile` |
| `Dicom.Edge.Hub.Infrastructure` | Integrates external systems: HL7 parsing, node HTTP push, background pipelines | `Hl7TcpListenerService`, `MessageDispatchHostedService`, `NodePushClient`, `HL7v2Parser` |
| `Dicom.Edge.Hub.Persistence` | EF Core data access, PostgreSQL migrations, repository implementations | `AppDbContext`, `StudyRepository`, `PatientRepository`, `Hl7MessageRepository`, EF migrations |
| `Dicom.Edge.Hub.Api` | ASP.NET Core host: REST API, SignalR, authentication middleware, rate limiting | `StudiesController`, `NodesController`, `EdgeHubNotificationHub`, `Program.cs` |
| `Dicom.Edge.Hub.Diagnostics` | Cross-cutting observability; consumed by Api and Infrastructure at startup | `SerilogBootstrap`, `PhiRedactionDestructuringPolicy`, `OpenTelemetryRegistration` |

---

## HL7 Inbound Pipeline

The Hub listens for HL7 v2 messages (ADT, ORM, ORU) over a raw TCP socket on port 8001 (MLLP). Messages flow through a staged pipeline before being committed to the database.

```mermaid
flowchart TB
    HIS[/"Hospital Information System (HIS/RIS)"/]
    LISTENER["Hl7TcpListenerService (IHostedService)<br/>Raw TCP socket · MLLP framing · Port 8001<br/>Sends ACK/NAK responses"]
    CHANNEL["System.Threading.Channels.Channel&lt;T&gt;<br/>Bounded, back-pressured in-process queue"]
    DISPATCH["MessageDispatchHostedService (IHostedService)<br/>Worker pool reading from Channel"]
    VALIDATE["HL7ValidationService<br/>Validates MSH, PID, PV1, OBR segments<br/>Rejects malformed messages with NAK"]
    PATIENT["PatientSyncService<br/>ADT^A01 / A08 / A40<br/>Create / update / merge patient"]
    STUDY["StudySyncService<br/>ORM^O01 / ORU^R01<br/>Create / update study records"]
    DB[("AppDbContext (PostgreSQL)<br/>hl7_messages · patients · studies")]

    HIS -- "Raw HL7 MLLP frame (TCP)" --> LISTENER
    LISTENER -- "Enqueue raw bytes" --> CHANNEL
    CHANNEL -- "Dequeue" --> DISPATCH
    DISPATCH --> VALIDATE
    VALIDATE -- "Valid message" --> PATIENT
    VALIDATE -- "Valid message" --> STUDY
    PATIENT --> DB
    STUDY --> DB

    classDef external fill:#ffe0b2,stroke:#ef6c00,color:#e65100
    classDef hub fill:#bbdefb,stroke:#1976d2,color:#0d47a1
    classDef db fill:#f8bbd0,stroke:#c2185b,color:#880e4f
    class HIS external
    class LISTENER,CHANNEL,DISPATCH,VALIDATE,PATIENT,STUDY hub
    class DB db
```

### Hosted Services

| Service | Interface | Role |
|---|---|---|
| `Hl7TcpListenerService` | `IHostedService` | Opens TCP socket; parses MLLP framing; sends HL7 ACK/NAK; enqueues raw messages to `Channel<RawHl7Message>` |
| `MessageDispatchHostedService` | `IHostedService` | Reads from the channel; dispatches to the appropriate sync service based on message type and trigger event |

---

## Hub → Node Push Pattern

When an administrator saves a PACS destination or routing rule through the Hub API, the change must be propagated to all relevant Edge Nodes without polling delays. The Hub uses a synchronous HTTP push via `NodePushClient`.

```mermaid
sequenceDiagram
    participant Admin as Admin (SPA)
    participant API as Hub API Controller
    participant Handler as Application Handler
    participant DB as PostgreSQL
    participant EvtHandler as Domain Event Handler
    participant Node as Node HTTP API

    Admin->>API: PUT /api/pacs-destinations/{id}
    API->>Handler: MediatR command
    Handler->>DB: Persist changes
    Handler->>EvtHandler: Raise PacsDestinationUpdatedEvent
    EvtHandler->>Node: HTTPS POST /api/pacs-destinations/sync
    EvtHandler->>Node: HTTPS POST /api/dicom-routing-rules/sync
    EvtHandler->>Node: HTTPS POST /api/configuration/sync
    Node-->>EvtHandler: 200 OK
```

> **Note:** If a Node is offline at the time of the push, the delivery is retried using a Polly retry pipeline (exponential back-off, configurable max retries). The Node will also re-sync on reconnection via its health-check handshake.

---

## Rate Limiting Policies

The Hub API enforces two named rate-limiting policies using ASP.NET Core's built-in rate limiter:

| Policy Name | Applied To | Limit |
|---|---|---|
| `api` | All `/api/*` endpoints | 200 requests per minute per client IP |
| `edge` | All `/api/edge/*` endpoints (Node→Hub communication) | 100 requests per minute per Node identity |

Requests that exceed the limit receive `HTTP 429 Too Many Requests` with a `Retry-After` header.

---

## Real-Time Notifications — SignalR

`EdgeHubNotificationHub` is a SignalR hub mounted at `/hubs/notifications`. The web SPA connects on startup and subscribes to typed events.

```mermaid
flowchart LR
    APP["Hub Application Layer"]
    NOTIFY["INotificationService.SendAsync(event)"]
    HUB["EdgeHubNotificationHub (SignalR)"]
    SPA[/"Connected SPA Clients (Browsers)"/]

    APP --> NOTIFY
    NOTIFY --> HUB
    HUB -- "WebSocket / SSE (HTTPS)" --> SPA

    classDef hub fill:#bbdefb,stroke:#1976d2,color:#0d47a1
    classDef external fill:#ffe0b2,stroke:#ef6c00,color:#e65100
    class APP,NOTIFY,HUB hub
    class SPA external
```

### Published Events

| Event | Trigger | SPA Reaction |
|---|---|---|
| `StudyReceived` | New study arrives from a Node | Update study list, increment badge counter |
| `StudyStatusChanged` | Study transitions between states | Update status chip in study table |
| `NodeStatusChanged` | Node connects or disconnects | Update node health indicator |
| `Hl7MessageProcessed` | HL7 message successfully parsed and stored | HL7 activity feed |

---

## Hub Database — PostgreSQL

The Hub uses a single PostgreSQL database (`edgeguard_hub`) managed by EF Core migrations. Key tables:

| Table | Purpose |
|---|---|
| `studies` | DICOM study records received from Edge Nodes |
| `patients` | Patient demographics, supports HL7 A40 merge |
| `hl7_messages` | Raw and parsed inbound HL7 messages |
| `nodes` | Registered Edge Node registry |
| `pacs_servers` | PACS destination definitions |
| `node_dicom_routing_rules` | Per-node DICOM routing rules pushed to nodes |
| `routing_rules` | Hub-level routing rules |
| `audit_logs` | Immutable audit trail for all mutations |
| `system_settings` | Key-value store for Hub configuration |
| `users` | Operator accounts with role assignments |

See [database-schema.md](./database-schema.md) for full column-level detail.
