# Glossary

This glossary defines terms used throughout the EdgeGuard Platform documentation. Terms are grouped thematically and cross-referenced where relevant.

---

## DICOM Protocol Terms

**DICOM** (Digital Imaging and Communications in Medicine)
: The international standard for the storage and transmission of medical images and related information. Maintained by NEMA. Every image, patient record, and structured report in the system conforms to a DICOM SOP Class.

**AE Title** (Application Entity Title)
: A string of up to 16 characters that uniquely identifies a DICOM application within a network. Examples: `EDGE_NODE_01`, `PACS_MAIN`. Used during association negotiation to route associations to the correct service.

**SOP Class** (Service-Object Pair Class)
: A combination of an Information Object Definition (IOD, e.g., CT Image) and a DICOM Service (e.g., Storage). Identified by a UID. Both SCU and SCP must support the same SOP Class for an operation to succeed.

**Transfer Syntax**
: A specific encoding rule for DICOM data, combining a pixel data compression scheme and a byte-order convention. Examples: `1.2.840.10008.1.2.1` (Explicit VR Little Endian), `1.2.840.10008.1.2.4.70` (JPEG Lossless). Negotiated during the Presentation Context phase of an association.

**Association**
: A DICOM "session" — a negotiated TCP connection between two AEs during which one or more DICOM operations are performed. Associations are short-lived and closed after the exchange completes.

**PDU** (Protocol Data Unit)
: The fundamental message unit exchanged over a DICOM association. Key PDUs: A-ASSOCIATE-RQ/AC/RJ (association negotiation), P-DATA-TF (payload), A-RELEASE-RQ/RP (graceful close), A-ABORT.

**SCU** (Service Class User)
: The DICOM role that initiates a service request. In a C-STORE operation, the modality is the SCU and the Edge Node is the SCP.

**SCP** (Service Class Provider)
: The DICOM role that responds to service requests. EdgeGuard Edge Nodes act as C-STORE SCP and C-FIND (MWL) SCP.

**C-STORE**
: The DICOM composite service used to transfer one SOP Instance (e.g., a single CT slice) from an SCU to an SCP. The primary mechanism by which modalities send images to the Edge Node.

**C-FIND**
: The DICOM query service used to search for matching records on an SCP. EdgeGuard nodes expose C-FIND as part of the Modality Worklist service.

**C-ECHO**
: The DICOM verification service, analogous to a network ping. Used to confirm that a DICOM association can be established. Run a C-ECHO from a PACS or modality to verify Edge Node connectivity.

**MWL** (Modality Worklist)
: A DICOM service (SOP Class `1.2.840.10008.5.1.4.31`) that allows modalities to query the Edge Node for scheduled procedures. The Edge Node serves the worklist data it received from the Hub, which in turn received it via HL7 ORM messages.

**StudyInstanceUID**
: A globally unique identifier (DICOM tag `0020,000D`) that identifies a study across all systems. Generated once at the modality and preserved throughout the workflow.

**AccessionNumber**
: A short identifier assigned by the RIS/HIS to a radiology order (DICOM tag `0008,0050`). Used to correlate DICOM studies with HL7 ORM orders and MWL entries.

**Worklist**
: The list of scheduled imaging procedures available on an MWL SCP. Modalities query the worklist before starting a scan to pre-populate patient demographics and procedure codes.

---

## HL7 v2.x Terms

**HL7** (Health Level Seven)
: A set of international standards for the exchange of clinical and administrative health information. EdgeGuard supports HL7 v2.x message types over MLLP.

**MLLP** (Minimal Lower Layer Protocol)
: The framing protocol used to transport HL7 v2.x messages over TCP. Each message is wrapped with a vertical-tab start-of-block character (0x0B), a carriage-return end-of-block (0x1C 0x0D), and a terminating byte. EdgeGuard's HL7 listener operates on TCP port 8001.

**MSH** (Message Header Segment)
: The mandatory first segment in every HL7 v2.x message. Contains the sending and receiving application/facility, message type, message control ID, processing ID, and version.

**PID** (Patient Identification Segment)
: HL7 segment containing patient identity information: internal ID, MRN, last name, first name, date of birth, sex, address. The Hub maps PID fields to its patient aggregate.

**ADT** (Admit, Discharge, Transfer)
: HL7 message type used to communicate patient movements. EdgeGuard processes `ADT^A01` (patient admit/register) and `ADT^A40` (patient merge — see MRG).

**ORM** (Order Message)
: HL7 message type used to communicate imaging orders. EdgeGuard processes `ORM^O01` to create or update worklist entries. New orders are pushed to the relevant Edge Node's MWL.

**ORU** (Observation Result)
: HL7 message type used to transmit diagnostic reports and results. EdgeGuard processes `ORU^R01` and extracts OBX segments for storage alongside the study record.

**OBX** (Observation/Result Segment)
: HL7 segment used within ORU messages to carry individual observation values (e.g., a diagnostic finding, a numeric result, or an encoded document).

**MRG** (Merge Patient Information Segment)
: HL7 segment present in `ADT^A40` messages. Contains the prior (source) patient identifier. EdgeGuard uses MRG to merge duplicate patient records in the Hub database.

**ACK** (Acknowledgment Message)
: The HL7 response message returned by the Hub's MLLP listener after processing an inbound message. An ACK with `MSA-1=AA` indicates acceptance; `AE` indicates an application error.

**NACK** (Negative Acknowledgment)
: Colloquial term for an ACK message with `MSA-1=AE` or `MSA-1=AR`, indicating that the sending system should not discard or retry the message without correction.

---

## Platform-Specific Terms

**Hub**
: The central server component of EdgeGuard Platform. Runs the ASP.NET Core API and serves the Angular SPA. Manages patients, studies, routing rules, PACS destinations, users, and HL7 events. Connects to PostgreSQL.

**Edge Node**
: A lightweight .NET Worker Service deployed at an imaging site. Receives DICOM studies from local modalities, stores them in SQLite, notifies the Hub, and forwards studies to PACS servers according to rules pushed by the Hub.

**PACS** (Picture Archiving and Communication System)
: The long-term storage and retrieval system for medical images. EdgeGuard forwards studies to one or more PACS servers via DICOM C-STORE.

**PHI** (Protected Health Information)
: Any health information that identifies an individual, as defined by HIPAA and equivalent regulations. EdgeGuard's Serilog pipeline automatically redacts PHI (patient names, MRNs, birth dates) from all log output.

**JWT** (JSON Web Token)
: The token format used for authentication in EdgeGuard. A signed, compact, URL-safe string containing claims (user ID, roles, expiry). Presented in the `Authorization: Bearer <token>` HTTP header.

**Bearer Token**
: An access token (JWT) included in HTTP requests to authenticate the caller. Short-lived (configurable via `Jwt:ExpiryMinutes`). Refreshed using a refresh token.

**Bootstrap Token**
: A one-time token printed to the Hub's log output on first startup. Used to create the initial administrator account before any users exist in the database. Invalidated after first use.

**Soft Delete**
: A deletion strategy in which records are marked with a `DeletedAt` timestamp rather than being physically removed from the database. Preserves audit history and enables recovery. All major Hub aggregates use soft delete.

**Aggregate Root**
: A Domain-Driven Design pattern. A cluster of domain objects treated as a single unit for data changes. Examples: `Patient`, `Study`, `RoutingRule`. All mutations go through the aggregate root to enforce invariants.

**Value Object**
: A DDD building block that is defined entirely by its attributes and has no identity of its own. Examples: `AeTitle`, `DicomUid`, `Hl7MessageType`. Value objects are immutable.

**Routing Rule**
: A Hub entity that maps a set of match criteria (Modality type, source AE Title, body part, study description pattern) to one or more PACS destination AE Titles. Rules are evaluated in priority order on the Hub and pushed to nodes.

**Rate Limiting**
: EdgeGuard enforces two named policies. The `edge` policy allows 100 requests/minute per client (used for node-to-Hub API calls). The `api` policy allows 200 requests/minute per client (used for SPA and external API consumers).

**SignalR**
: Microsoft's real-time web framework used by the Hub to push study-received and delivery-confirmed events to connected Angular SPA clients over WebSocket.

**Serilog**
: The structured logging library used throughout EdgeGuard. Log entries are written in JSON with PHI redaction applied before any sink (console, file, Seq, OpenTelemetry) receives them.

**Seq**
: An optional centralized log server that ingests structured JSON logs from Serilog. Provides a web UI for querying and alerting. Configured with the `Seq:ServerUrl` key.

**OpenTelemetry**
: An observability framework for collecting distributed traces, metrics, and logs. EdgeGuard exports OTLP traces and metrics when an OpenTelemetry endpoint is configured.

**Clean Architecture**
: The layering pattern used in the Hub API: `Domain` (entities, value objects, domain events) → `Application` (application services, validation, DTO mapping) → `Infrastructure` (EF Core, HL7 parser, DICOM client, domain event handlers) → `Api` (controllers, middleware, DI composition). Dependencies point inward only.

**Read/Write Split**
: The lightweight separation used in the Hub Application layer. Read operations go directly through repository interfaces (`IStudyRepository.GetByIdAsync`, …); write operations go through application service interfaces (`IStudyService`, `INodeService`, …) that orchestrate the change and commit it via the Unit of Work. There is **no** MediatR / command-handler bus — domain events are dispatched after `SaveChanges` by `DomainEventDispatchInterceptor`.
