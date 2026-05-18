# EdgeGuard Platform — Documentation

EdgeGuard Platform is an enterprise medical imaging hub built on a hub-and-spoke architecture: a central **Hub** aggregates DICOM studies and HL7 workflow events from distributed **Edge Nodes** deployed at imaging sites, forwards studies to one or more PACS servers according to configurable routing rules, and exposes a unified Angular SPA for clinical and administrative users. The system supports the full DICOM service class hierarchy (C-STORE, C-FIND, C-ECHO, Modality Worklist), the HL7 v2.x message types most common in radiology workflows (ADT, ORM, ORU), and a secure REST API protected by JWT authentication — all designed to operate in regulated healthcare environments that require PHI redaction in logs and strict audit trails.

---

## Quick Navigation

| # | Section | What you will find |
|---|---------|-------------------|
| [01](01-overview/architecture.md) | **Architecture** | C4 context & container diagrams, data-flow description, hub↔node sync pattern |
| [01](01-overview/glossary.md) | **Glossary** | Definitions for 35+ DICOM, HL7, and platform-specific terms |
| [01](01-overview/changelog.md) | **Changelog** | Version history from v0.5.0 through v1.0.0 |
| [02](02-getting-started/prerequisites.md) | **Prerequisites** | Runtime, database, network, and hardware requirements |
| [02](02-getting-started/quickstart-hub.md) | **Quickstart — Hub** | End-to-end Hub setup in under 15 minutes |
| [02](02-getting-started/quickstart-node.md) | **Quickstart — Edge Node** | Edge Node installation, registration, and first C-ECHO test |
| [02](02-getting-started/environment-variables.md) | **Environment Variables** | Complete reference table of all configuration keys |
| 03 | **Architecture Deep-Dive** | Domain model, Clean Architecture layers, DDD aggregates |
| 04 | **Features** | Routing rules, multi-PACS, SignalR notifications, WhatsApp alerts |
| 05 | **API Reference** | OpenAPI endpoints, request/response schemas, error codes |
| 06 | **SPA Guide** | Angular standalone components, state management, theming |
| 07 | **Operations** | Windows Server + IIS (Hub), Windows Service (Node), monitoring, backup; Docker / systemd as alternatives |
| 08 | **Integrations** | HL7 pipeline details, DICOM conformance, PACS compatibility |
| 09 | **Development** | Contribution guide, coding standards, testing strategy |

---

## Where to Start

### Operator / IT Administrator
1. Read [Prerequisites](02-getting-started/prerequisites.md) to verify your environment.
2. Follow [Quickstart — Hub](02-getting-started/quickstart-hub.md) to stand up the central server.
3. Follow [Quickstart — Edge Node](02-getting-started/quickstart-node.md) for each imaging site.
4. Review [Environment Variables](02-getting-started/environment-variables.md) for production hardening.

### Application Developer
1. Skim the [Architecture](01-overview/architecture.md) for the big picture.
2. Read the [Glossary](01-overview/glossary.md) to align on terminology.
3. Set up a local Hub instance via [Quickstart — Hub](02-getting-started/quickstart-hub.md).
4. Consult sections 03 (domain model), 05 (API), and 09 (development guide) — located in their respective subdirectories.

### HL7 / DICOM Integrator
1. Start with the [Glossary](01-overview/glossary.md) for protocol terminology.
2. Review [Architecture](01-overview/architecture.md) for the data-flow description.
3. Consult section 08 (Integrations) for HL7 message type support, MLLP configuration, and DICOM conformance statements.
4. Use [Environment Variables](02-getting-started/environment-variables.md) to configure ports and listener settings.

---

## Stack Summary

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend API | ASP.NET Core (Clean Architecture) | .NET 10 |
| Edge Worker | .NET Worker Service | .NET 10 |
| DICOM library | fo-dicom | latest stable |
| Frontend SPA | Angular (standalone components) | Angular 19 |
| Hub database | PostgreSQL | 16+ |
| Node database | SQLite (bundled) | 3.x |
| HL7 transport | MLLP over TCP | HL7 v2.x |
| Authentication | JWT + refresh tokens | — |
| Logging | Serilog (PHI-redacted) + optional Seq / OpenTelemetry | — |

### Deployment Targets

| Component | Primary target (production) | Alternative |
|-----------|------------------------------|-------------|
| **Hub** | 🪟 **Windows Server + IIS** (ASP.NET Core Hosting Bundle, App Pool `EdgeGuardHub`, AspNetCoreModuleV2 in-process) + **PostgreSQL 16** | Linux + Kestrel behind Nginx, or Docker Compose |
| **Edge Node** | 🪟 **Windows Service `EdgeGuardNode`** (`C:\EdgeGuard\Node\`) + bundled **SQLite** (Kestrel binds directly on `:5001`, DICOM SCP on `:11112`) | Linux + systemd, or Docker container |

See [deployment-hub.md](07-operations/deployment-hub.md) and [deployment-node.md](07-operations/deployment-node.md) for step-by-step install runbooks.

---

> **Note:** All PHI (Protected Health Information) is redacted from logs at the transport layer. The Hub operates in `Relaxed` redaction mode by default; Edge Nodes default to `Strict`. Both modes are configurable — see [Environment Variables](02-getting-started/environment-variables.md).
