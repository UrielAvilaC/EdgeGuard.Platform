# Architecture

This document describes the EdgeGuard Platform architecture at the context and container levels, followed by key data-flow narratives and the hub-to-node synchronization pattern.

> **Important — DICOM routing model:**
> Imaging modalities **only connect to Edge Nodes** (DICOM C-STORE / C-FIND MWL on port `11112` by default).
> The Hub **never** receives DICOM associations from modalities. The Hub communicates with nodes via HTTPS REST and with HIS/RIS via HL7 MLLP.

---

## C4 Level 1 — System Context

```mermaid
flowchart LR
    HIS["HIS / RIS<br/>(Hospital / Radiology<br/>Information System)"]
    Modality["Modality<br/>(CT / MR / CR / DX / US)"]
    Clinician["Clínicos / Radiólogos<br/>(Web browser)"]
    PACS["PACS<br/>(target archive)"]

    subgraph Platform["EdgeGuard Platform"]
        Hub["Hub Central<br/>ASP.NET Core API + Angular SPA<br/>PostgreSQL"]
        Node["Edge Node (N instances)<br/>.NET Worker + fo-dicom + SQLite"]
    end

    HIS -- "HL7 v2.x / MLLP<br/>TCP :8001" --> Hub
    Clinician -- "HTTPS REST + WSS<br/>:5000 / :5001" --> Hub
    Modality -- "DICOM C-STORE / C-FIND MWL<br/>TCP :11112" --> Node
    Node -- "HTTPS REST<br/>(Node → Hub: notifications)" --> Hub
    Hub -- "HTTPS REST<br/>(Hub → Node: config push)" --> Node
    Node -- "DICOM C-STORE SCU<br/>TCP :104 / :11112" --> PACS

    classDef platform fill:#e1f5ff,stroke:#0277bd,stroke-width:2px
    classDef external fill:#fff3e0,stroke:#e65100,stroke-width:1px
    class Hub,Node platform
    class HIS,Modality,Clinician,PACS external
```

**External actors:**

| Actor | Role | Protocol → EdgeGuard target |
|---|---|---|
| **HIS / RIS** | Hospital/Radiology Information System (HL7 producer) | MLLP over TCP → **Hub** (`:8001`) |
| **Modality** | Imaging equipment (CT, MR, CR, DX, US, …) | DICOM C-STORE / C-FIND → **Edge Node** (`:11112`) |
| **Clínicos** | Clinical users accessing the Angular SPA | HTTPS + WSS → **Hub** (`:5000` / `:5001`) |
| **PACS** | Picture Archiving and Communication System (study destination) | DICOM C-STORE SCU from **Edge Node** |

---

> **Production deployment target:** Hub runs on **Windows Server + IIS** (ASP.NET Core Hosting Bundle + AspNetCoreModuleV2). Each Edge Node runs as a **Windows Service** (`sc.exe` / `New-Service`). See [deployment-hub.md](../07-operations/deployment-hub.md) and [deployment-node.md](../07-operations/deployment-node.md). Linux/Docker deployments are supported as a secondary option.

## C4 Level 2 — Containers

```mermaid
flowchart TB
    subgraph HubBox["Hub Central"]
        direction TB
        API["ASP.NET Core Web API<br/>(Clean Architecture)<br/>• REST endpoints<br/>• SignalR hub<br/>• HL7 MLLP listener :8001<br/>• JWT auth + Rate limiter"]
        SPA["Angular 19 SPA<br/>(standalone components)<br/>served from /wwwroot"]
        PG[("PostgreSQL 16<br/>HUB_DB_CONNECTION_STRING")]
        API -- "serves" --> SPA
        API -- "reads / writes" --> PG
    end

    subgraph NodeBox["Edge Node (deployed at imaging site, N instances)"]
        direction TB
        Worker[".NET Worker Service<br/>• fo-dicom C-STORE SCP :11112<br/>• fo-dicom C-FIND MWL :11112<br/>• fo-dicom C-ECHO :11112<br/>• Routing rule engine<br/>• ASP.NET Core management API"]
        SQLite[("SQLite<br/>./persistence/edge-node.db")]
        Worker -- "reads / writes" --> SQLite
    end

    Modality["Modality<br/>(CT / MR / CR …)"]
    HIS["HIS / RIS"]
    PACS["PACS"]
    Browser["Clínico<br/>(Browser)"]

    Modality -- "DICOM :11112" --> Worker
    HIS -- "MLLP :8001" --> API
    Browser -- "HTTPS / WSS" --> API
    API -. "Hub → Node push<br/>(PACS / Rules / Config)" .-> Worker
    Worker -. "Node → Hub<br/>(Study events / Telemetry)" .-> API
    Worker -- "C-STORE SCU" --> PACS

    classDef hub fill:#e3f2fd,stroke:#1565c0
    classDef node fill:#f1f8e9,stroke:#558b2f
    classDef external fill:#fff3e0,stroke:#e65100
    classDef db fill:#fce4ec,stroke:#ad1457
    class API,SPA hub
    class Worker node
    class PG,SQLite db
    class Modality,HIS,PACS,Browser external
```

### Container Responsibilities

| Container | Responsibility |
|-----------|----------------|
| **ASP.NET Core Web API (Hub)** | Business logic, HL7 message parsing, central routing rule storage, REST API, SignalR real-time events, JWT issuance and validation |
| **Angular SPA** | Clinical dashboard, node management, routing rule configuration, study browser, alert configuration |
| **PostgreSQL** | Persistent store for patients, studies, routing rules, PACS destinations, users, HL7 messages, and audit events |
| **Edge Node Worker** | DICOM C-STORE SCP (receives studies from modalities), MWL C-FIND SCP (serves worklist), local rule evaluation, study forwarding to PACS, Hub notification |
| **SQLite (per node)** | Local persistence for received instances, node configuration cache, and offline queue |

---

## Data Flow — Image Arrival to PACS Delivery

```mermaid
sequenceDiagram
    autonumber
    participant M as Modality
    participant N as Edge Node<br/>(fo-dicom :11112)
    participant H as Hub Central
    participant DB as PostgreSQL
    participant P as PACS
    participant U as SPA (Browser)

    M->>N: C-STORE (DICOM instances)
    N->>N: Persist instances<br/>(SQLite + filesystem)
    N->>H: POST /api/edge/studies<br/>(study received)
    H->>DB: UPSERT study (Receiving)
    H-->>U: SignalR: StudyStatusChanged
    H->>N: PUT /api/dicom-routing-rules/sync<br/>(if rules pending)
    N->>N: Evaluate rules<br/>(modality, AE, instances)
    N->>P: C-STORE SCU (forward study)
    P-->>N: C-STORE response (Success)
    N->>H: POST /api/edge/studies/confirm<br/>(delivery success)
    H->>DB: UPDATE study (Completed)
    H-->>U: SignalR: StudyStatusChanged
```

**Steps in detail:**

1. **Image arrival** — A modality opens a DICOM association with the Edge Node's C-STORE SCP on the configured port (default `11112`). Instances are written to SQLite + filesystem as they arrive; the association is closed after the final instance.
2. **Hub notification** — The Edge Node posts a study-received event to the Hub REST API. The Hub persists the study metadata in PostgreSQL and broadcasts a SignalR notification to connected SPA clients.
3. **Routing rule evaluation** — The Node's local rule engine evaluates the study against the rules previously pushed by the Hub (matching by modality, source AE Title, institution, instance count, etc.) and selects the destination PACS.
4. **PACS forwarding** — The Edge Node opens a C-STORE SCU association with the target PACS and sends all instances. Transfer syntax negotiation follows the standard DICOM association handshake.
5. **Delivery confirmation** — After a successful PACS C-STORE response, the Edge Node posts a delivery-confirmed event to the Hub. The Hub updates the study record to `Completed` and surfaces the status in the SPA in real time via SignalR.

---

## Hub → Node Push Pattern

The Hub is the authoritative source for routing rules, PACS destination configuration, and operational settings. Nodes **do not poll**; instead, the Hub pushes updates proactively over HTTPS to each node's management API.

```mermaid
sequenceDiagram
    autonumber
    participant A as Admin (SPA)
    participant H as Hub API
    participant N as Edge Node API

    A->>H: PUT /api/nodes/{id}/dicom-routing-rules
    H->>H: Persist in PostgreSQL
    H->>N: POST /api/dicom-routing-rules/sync<br/>(payload: full rule set)
    N->>N: Replace local SQLite rule set
    N-->>H: 200 OK
    H-->>A: 200 OK

    Note over H,N: Same flow for:<br/>• /api/pacs-destinations/sync<br/>• /api/configuration/sync

    rect rgb(255, 240, 230)
        Note over H,N: Offline handling
        H->>N: POST /api/.../sync
        N--xH: Connection refused
        H->>H: Queue update with exp. back-off
        N->>H: GET /api/nodes/{id}/config (reconnect)
        H-->>N: Full configuration snapshot
    end
```

> **Note:** If a node is temporarily offline when a push is attempted, the Hub queues the update and retries with exponential back-off. The node also fetches its full configuration on reconnect via `GET /api/nodes/{id}/config`.

---

## Security Boundaries

```mermaid
flowchart LR
    subgraph Internet["Internet / Hospital LAN"]
        HIS["HIS / RIS"]
        Browser["SPA Browser"]
    end

    subgraph DMZ["DMZ / Internal Network"]
        Hub["Hub Central"]
    end

    subgraph SiteNet["Imaging Site Network"]
        Node["Edge Node"]
        Modality["Modality"]
        PACS["PACS"]
    end

    HIS -- "MLLP (firewall)" --> Hub
    Browser -- "HTTPS + JWT" --> Hub
    Modality -- "DICOM<br/>(AE Title allow-list)" --> Node
    Node -- "HTTPS + JWT<br/>(node identity)" --> Hub
    Hub -. "HTTPS + pre-shared secret" .-> Node
    Node -- "DICOM<br/>(Called AE)" --> PACS

    classDef secure fill:#e8f5e9,stroke:#2e7d32
    classDef perimeter fill:#fff8e1,stroke:#f9a825
    class Hub,Node secure
    class HIS,Browser,Modality,PACS perimeter
```

| Boundary | Protocol | Auth |
|----------|----------|------|
| HIS/RIS → Hub | MLLP over TCP | Network-level (firewall + IP allow-list) |
| Modality → Edge Node | DICOM association `:11112` | AE Title allow-list (Called AE + optional Calling AE) |
| Edge Node → Hub | HTTPS REST | JWT (node identity token) |
| SPA → Hub | HTTPS REST + WSS | JWT (user Bearer token) |
| Hub → Edge Node (push) | HTTPS REST | Pre-shared node secret |
| Edge Node → PACS | DICOM C-STORE SCU `:104` / `:11112` | AE Title + Called AE configuration |
