# Edge Node — Component Overview

An EdgeGuard Edge Node is a lightweight, autonomous service deployed on-premises at a clinical site. It receives DICOM studies directly from imaging modalities on port 11112, applies local routing rules, and forwards studies to PACS destinations. It operates in offline-first mode and synchronises state with the Hub when connectivity is available.

---

## Project Relationship Diagram

```mermaid
flowchart TB
    subgraph Node["EdgeGuard Edge Node"]
        WORKER["Dicom.Edge.Node (Worker)<br/>.NET Worker host · DI root<br/>Hosted service orchestration"]
        API["Dicom.Edge.Node.Api<br/>/health · /api/pacs-destinations/sync<br/>/api/dicom-routing-rules/sync<br/>/api/configuration/sync"]
        CFG["Node.Configuration<br/>App settings · Typed options"]
        DICOMSRV["Node.DicomServer<br/>CStoreScp · fo-dicom SCP<br/>Port 11112"]
        PROC["Node.Processing<br/>DicomInstanceHandler<br/>Tag extraction · Study aggregation"]
        QUEUE["Node.Queue<br/>StudyQueue (in-process)"]
        ROUTER["Node.Router<br/>Routing rule evaluation<br/>Destination selection"]
        SENDER["Node.Sender<br/>DICOM C-STORE SCU<br/>Forward to PACS"]
        WORKLIST["Node.Worklist<br/>WorklistCFindHandler (MWL)"]
        STORAGE["Node.Storage<br/>DICOM file store<br/>Local filesystem"]
        PERS[("Node.Persistence<br/>SQLite · EF Core<br/>node_studies · node_pacs_servers<br/>node_routing_rules · node_settings")]
        DIAG["Node.Diagnostics<br/>Serilog · PHI Strict redaction<br/>Health checks"]
    end

    WORKER --> API
    WORKER --> DICOMSRV
    WORKER --> WORKLIST
    API --> CFG
    API --> PROC
    API --> ROUTER
    DICOMSRV --> PROC
    PROC --> QUEUE
    PROC --> STORAGE
    QUEUE --> ROUTER
    ROUTER --> SENDER
    WORKLIST --> PERS
    PROC --> PERS
    ROUTER --> PERS
    SENDER --> PERS
    WORKER --> DIAG

    classDef node fill:#c8e6c9,stroke:#388e3c,color:#1b5e20
    classDef db fill:#f8bbd0,stroke:#c2185b,color:#880e4f
    class WORKER,API,CFG,DICOMSRV,PROC,QUEUE,ROUTER,SENDER,WORKLIST,STORAGE,DIAG node
    class PERS db
```

---

## Project Reference Table

| Project | Responsibility | Key Classes |
|---|---|---|
| `Dicom.Edge.Node` | .NET Worker host; DI composition root; hosts all other services | `Program.cs`, `WorkerHostExtensions` |
| `Dicom.Edge.Node.Api` | Lightweight ASP.NET Core API for Hub push synchronisation and health | `PacsDestinationsSyncController`, `DicomRoutingRulesSyncController`, `ConfigurationSyncController`, `HealthController` |
| `Dicom.Edge.Node.Configuration` | Reads and validates `appsettings.json`; typed options classes | `NodeOptions`, `DicomServerOptions`, `HubConnectionOptions` |
| `Dicom.Edge.Node.DicomServer` | fo-dicom SCP; accepts C-STORE and C-ECHO associations from modalities on port 11112 | `CStoreScp`, `DicomServer`, `AeTitleValidator`, `PresentationContextNegotiator` |
| `Dicom.Edge.Node.Processing` | Handles each received DICOM instance; builds study aggregation | `DicomInstanceHandler`, `StudyAggregator`, `DicomTagExtractor` |
| `Dicom.Edge.Node.Queue` | In-process queue bridging receive and routing; prevents back-pressure on SCP | `StudyQueue`, `StudyQueueItem`, `QueueMonitorService` |
| `Dicom.Edge.Node.Router` | Evaluates routing rules against study attributes; selects destinations | `RoutingRuleEngine`, `RoutingRuleEvaluator`, `DestinationResolver` |
| `Dicom.Edge.Node.Sender` | DICOM C-STORE SCU; forwards studies to PACS; handles retries | `DicomStoreScu`, `SendStudyJob`, `RetryPolicy` |
| `Dicom.Edge.Node.Storage` | Stores received DICOM files locally; garbage collection of sent files | `LocalDicomFileStore`, `StorageCleanupService` |
| `Dicom.Edge.Node.Worklist` | MWL SCP; responds to C-FIND requests from modalities | `WorklistCFindHandler`, `WorklistQueryBuilder` |
| `Dicom.Edge.Node.Persistence` | SQLite data access via EF Core; offline-first local state | `NodeDbContext`, `NodeStudyRepository`, `NodeRoutingRuleRepository` |
| `Dicom.Edge.Node.Diagnostics` | Serilog with PHI Strict mode; health check registrations | `NodeSerilogBootstrap`, `PhiStrictDestructuringPolicy` |

---

## DICOM Receive Flow (C-STORE)

Modalities (CT, MR, CR, DX, US, etc.) connect directly to the Edge Node on port 11112 via DICOM. They never connect to the Hub.

```mermaid
flowchart TB
    MODALITY[/"Imaging Modality (CT, MR, CR, DX, US, …)"/]
    SCP["CStoreScp (fo-dicom)<br/>Node.DicomServer · Port 11112<br/>· AE title validation<br/>· Presentation context negotiation<br/>· C-ECHO / C-STORE"]
    HANDLER["DicomInstanceHandler<br/>Node.Processing<br/>· Extract DICOM tags<br/>· Accumulate instances into study<br/>· Persist to SQLite"]
    QUEUE["StudyQueue · Node.Queue<br/>Bounded Channel&lt;T&gt;<br/>Decouples SCP from routing latency"]
    ROUTER["RoutingRuleEngine · Node.Router<br/>· Evaluate rules by priority<br/>· Match Modality, SourceAeTitle, Institution,<br/>  StudyDescription, InstanceCount range<br/>· Select destination(s)"]
    SCU["DicomStoreScu · Node.Sender<br/>· Open association to PACS destination<br/>· C-STORE each instance<br/>· Close association · Retry on failure"]
    PACS[/"PACS Destination"/]
    HUB[/"Hub (if SendToHub = true)"/]

    MODALITY -- "DICOM Association Request<br/>(TCP, AE title negotiation)" --> SCP
    SCP -- "DICOM instance (file + dataset)" --> HANDLER
    HANDLER -- "StudyQueueItem" --> QUEUE
    QUEUE -- "Dequeue when complete/timeout" --> ROUTER
    ROUTER -- "Resolved destination(s)" --> SCU
    SCU --> PACS
    SCU --> HUB

    classDef external fill:#ffe0b2,stroke:#ef6c00,color:#e65100
    classDef node fill:#c8e6c9,stroke:#388e3c,color:#1b5e20
    class MODALITY,PACS,HUB external
    class SCP,HANDLER,QUEUE,ROUTER,SCU node
```

---

## Modality Worklist (MWL) Flow

```mermaid
flowchart TB
    MODALITY[/"Imaging Modality"/]
    SCP["MWL SCP · Node.DicomServer · Port 11112<br/>Identifies C-FIND for Modality Worklist"]
    HANDLER["WorklistCFindHandler · Node.Worklist<br/>· Parse query dataset<br/>· Build SQLite query from C-FIND keys<br/>  (PatientName, Date, Modality, AeTitle…)<br/>· Return matching items"]
    MODALITY2[/"Imaging Modality<br/>(pre-populates patient/exam fields)"/]

    MODALITY -- "DICOM Association Request<br/>(MWL SCP, C-FIND)" --> SCP
    SCP -- "C-FIND-RQ dataset" --> HANDLER
    HANDLER -- "C-FIND-RSP dataset(s)" --> MODALITY2

    classDef external fill:#ffe0b2,stroke:#ef6c00,color:#e65100
    classDef node fill:#c8e6c9,stroke:#388e3c,color:#1b5e20
    class MODALITY,MODALITY2 external
    class SCP,HANDLER node
```

> **Note:** Worklist data is populated by the HL7 ORM^O01 pipeline on the Hub. The Hub pushes scheduled procedure steps to the Node via `/api/configuration/sync` when worklist entries are created or modified.

---

## Hub Synchronisation — Node API Endpoints

The Node exposes a minimal HTTP API exclusively for Hub-initiated pushes. The Node does not poll the Hub; the Hub pushes changes as they occur.

| Endpoint | Method | Purpose | Payload |
|---|---|---|---|
| `/health` | GET | Liveness and readiness probe | `{ status, version, nodeId, connectedAt }` |
| `/api/pacs-destinations/sync` | POST | Receive updated PACS destination list from Hub | `PacsDestinationSyncRequest` |
| `/api/dicom-routing-rules/sync` | POST | Receive updated routing rules from Hub | `DicomRoutingRuleSyncRequest` |
| `/api/configuration/sync` | POST | Receive general node configuration and worklist entries | `ConfigurationSyncRequest` |

Requests from the Hub are authenticated using a shared node token (HMAC-signed, configured at node registration). The token is validated by the Node API middleware before any handler is invoked.

---

## SQLite Persistence — Offline-First Model

The Node persists all state to a local SQLite database so it can operate independently of Hub connectivity.

```mermaid
flowchart LR
    subgraph Online["Hub Connected"]
        O1["Push → Node updates config"]
        O2["Push → Node updates PACS destinations"]
        O3["Push → Node updates routing rules"]
        O4["Node sends study to PACS"]
        O5["Node reports study to Hub"]
    end

    subgraph Offline["Hub Offline"]
        F1["Node uses last-known config"]
        F2["Node routes to known destinations"]
        F3["Node applies cached rules"]
        F4["Node sends study to PACS"]
        F5["Node queues report for retry"]
    end

    Online -. "reconnect" .-> Offline
    Offline -. "reconnect" .-> Online

    classDef node fill:#c8e6c9,stroke:#388e3c,color:#1b5e20
    class O1,O2,O3,O4,O5,F1,F2,F3,F4,F5 node
```

When the Hub reconnects (detected via health-check handshake), the Node flushes its pending report queue and the Hub pushes any configuration changes that occurred during the offline period.

### Node SQLite Tables

| Table | Purpose |
|---|---|
| `node_studies` | Local record of received studies and their send status |
| `node_pacs_servers` | PACS destination definitions synced from Hub |
| `node_routing_rules` | Routing rules synced from Hub |
| `node_settings` | Key-value node configuration (AE title, port, Hub URL, token) |

---

## fo-dicom Association Lifecycle

Each DICOM association from a modality goes through a well-defined lifecycle managed by the `CStoreScp` in `Node.DicomServer`:

```mermaid
sequenceDiagram
    participant M as Modality
    participant SCP as Node CStoreScp (port 11112)

    M->>SCP: A-ASSOCIATE-RQ<br/>(Calling AE, Called AE, Presentation Contexts)
    Note over SCP: 1. Validate Calling AE title against allowed list
    Note over SCP: 2. Negotiate presentation contexts<br/>(SOP class + transfer syntax)
    SCP-->>M: A-ASSOCIATE-AC (Accepted contexts)

    M->>SCP: C-STORE-RQ (instance 1)
    Note over SCP: 3. Receive instance<br/>Store to local filesystem<br/>Hand off to DicomInstanceHandler
    SCP-->>M: C-STORE-RSP (0000H success)

    M->>SCP: C-STORE-RQ (instance N)
    SCP-->>M: C-STORE-RSP (0000H success)

    M->>SCP: A-RELEASE-RQ
    Note over SCP: 4. Release association<br/>Trigger study completion check<br/>Enqueue to StudyQueue
    SCP-->>M: A-RELEASE-RSP
```

If the Calling AE title is not in the Node's allowed list, the SCP responds with `A-ASSOCIATE-RJ` (reason: Calling AE title not recognised). All association events are logged (with PHI redacted in Strict mode).

---

## Node Diagnostics and Health

`Dicom.Edge.Node.Diagnostics` wires up:

- **Serilog** with PHI Strict redaction (all patient identifiers replaced with `[REDACTED]` in log output)
- **OpenTelemetry** traces exported to Hub's collector endpoint
- **Health checks**: DICOM server reachability, SQLite connectivity, Hub connectivity, disk space for DICOM storage

Health check results are exposed at `/health` and also pushed to the Hub's `EdgeHubNotificationHub` SignalR channel so the dashboard reflects node status in real time.
