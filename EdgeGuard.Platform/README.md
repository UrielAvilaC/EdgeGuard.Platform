# EdgeGuard Platform

Enterprise DICOM medical imaging platform for edge-to-cloud study management, HL7 integration, and patient notification delivery.

---

## 🏗️ Architecture

```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   Edge Node(s)   │────▶│    Hub (API)      │────▶│   PACS Server    │
│  DICOM C-STORE   │     │  HL7 Listener     │     │                  │
│  Worklist (MWL)  │     │  Study Pipeline   │     │                  │
└──────────────────┘     │  WhatsApp Engine   │     └──────────────────┘
                         └──────────────────┘
                                │
                         ┌──────┴──────┐
                         │  PostgreSQL  │
                         └─────────────┘
```

| Layer | Project | Description |
|-------|---------|-------------|
| API | `Dicom.Edge.Hub.Api` | ASP.NET Core REST API |
| Application | `Dicom.Edge.Hub.Application` | Business logic, orchestration |
| Domain | `Dicom.Edge.Hub.Domain` | Aggregates, entities, value objects |
| Infrastructure | `Dicom.Edge.Hub.Infrastructure` | HL7 listener, Twilio, hosted services |
| Persistence | `Dicom.Edge.Hub.Persistence` | EF Core, PostgreSQL, repositories |
| Edge | `Dicom.Edge.Node` | DICOM server, worklist, study processing |
| Shared | `Dicom.Edge.*` | Contracts, models, security, abstractions |
| Frontend | `dicomedge-ui` | Angular UI |

---

## 📱 WhatsApp Messaging Module

Enterprise-grade WhatsApp notification system for automated and manual study result delivery via Twilio (multi-provider ready).

### Features

| Feature | Description |
|---------|-------------|
| **Automatic Delivery** | When a study transitions to a configured status (e.g. `SentToPacs`), the system automatically sends a WhatsApp message to the patient's registered phone number |
| **Manual Send** | Operators can send WhatsApp messages to arbitrary recipients for any study via `POST /api/whatsapp/send` |
| **Content Templates** | Templates map to Twilio Content SIDs with ordered positional variables resolved at send-time |
| **Auto-Send Rules** | Admin configures which `StudyStatus` triggers which template. One rule per status, unique constraint |
| **Phone Normalization** | 10-digit numbers automatically get `+521` (Mexico mobile) prefix. Configurable via `whatsapp.default_country_prefix` |
| **HL7 Patient Sync** | Phone and email are extracted from HL7 PID-13/PID-14 and synced to the Patient record automatically |
| **Multi-Provider** | `IMessagingProvider` abstraction — Twilio today, Meta tomorrow. Zero changes in Application/Domain layers |
| **Encrypted Config** | Provider credentials stored as encrypted JSON using ASP.NET Core Data Protection with automatic key rotation |
| **Full Audit Trail** | Every send, skip, failure, template change, and rule change is recorded in `hub_audit_logs` |

### System Settings

| Key | Default | Type | Description |
|-----|---------|------|-------------|
| `whatsapp.enabled` | `false` | bool | Global kill switch |
| `whatsapp.enable_automatic_delivery` | `false` | bool | Enable auto-send on study status change |
| `whatsapp.provider` | `Twilio` | string | Active messaging provider (`Twilio` \| `Meta`) |
| `whatsapp.provider_config` | `{}` | encrypted | JSON: `{ AccountSid, AuthToken, PhoneNumber, MessagingServiceSid }` |
| `whatsapp.default_country_prefix` | `+521` | string | Prepended to 10-digit phone numbers |
| `whatsapp.retry_max_attempts` | `3` | int | Max retry attempts for failed notifications |
| `whatsapp.retry_delay_seconds` | `60` | int | Delay between background retry cycles |

### API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/whatsapp/config-status` | Configuration status overview |
| `GET` | `/api/whatsapp/tags` | Available template variable tags |
| `GET` | `/api/whatsapp/templates` | List all templates |
| `GET` | `/api/whatsapp/templates/{id}` | Template detail with variables |
| `POST` | `/api/whatsapp/templates` | Create template |
| `PUT` | `/api/whatsapp/templates/{id}` | Update template |
| `DELETE` | `/api/whatsapp/templates/{id}` | Delete template |
| `GET` | `/api/whatsapp/auto-send-rules` | List auto-send rules |
| `POST` | `/api/whatsapp/auto-send-rules` | Create rule (StudyStatus → Template) |
| `PUT` | `/api/whatsapp/auto-send-rules/{id}` | Update rule |
| `DELETE` | `/api/whatsapp/auto-send-rules/{id}` | Delete rule |
| `POST` | `/api/whatsapp/send` | Manual send to recipients |
| `GET` | `/api/whatsapp/notifications/by-study/{studyId}` | Notification history |

### Template Variables

Variables are resolved at send-time from the study and patient context:

| Tag | Source | Example |
|-----|--------|---------|
| `accessionNumber` | Study.AccessionNumber | `ACC-2025-001` |
| `patientCode` | Patient.PatientDicomId | `PAT12345` |
| `patientName` | Study.PatientName | `John Doe` |
| `studyDate` | Study.StudyDate | `15/07/2025` |
| `appointmentDate` | Study.WorklistReadAt | `20/07/2025` |
| `studyDescription` | Study.StudyDescription | `CT Abdomen` |
| `modality` | StudySeries.Modality | `CT` |
| `referringPhysician` | Study.ReferringPhysician | `Dr. Smith` |
| `institutionName` | Configuration | `General Hospital` |
| `pacsViewerLink` | Generated | `https://pacs.example.com/view/123` |
| `imagesUrl` | Generated | `https://images.example.com/study/123` |

### Automatic Delivery Flow

```
Study changes status (e.g. Completed → SentToPacs)
  │
  ├── whatsapp.enabled == false? → stop
  ├── whatsapp.enable_automatic_delivery == false? → stop
  ├── AutoSendRule for this status exists & enabled? → no → stop
  ├── Load Template + Variables
  ├── Load Patient from Study
  ├── Patient has phone? → no → audit "Skipped: No phone" → stop
  ├── Normalize phone (10 digits → +521XXXXXXXXXX)
  │     → invalid? → audit "Skipped: Invalid phone" → stop
  ├── Resolve template variables from Study/Patient
  ├── Send via IMessagingProvider (Twilio)
  ├── Success → MarkSent + audit WhatsAppNotificationSent
  └── Failure → MarkFailed + audit WhatsAppNotificationFailed
```

### HL7 PID Extraction

Phone and email are automatically extracted from HL7 v2.x PID segments:

```
PID|||12345||Doe^John||19850315|M|||||5551234567^PRN^PH~user@email.com^NET^Internet
                                       │                  │
                                       PID-13.1: Phone    PID-13 repetition: Email
```

- **Phone**: PID-13 component 1 (first repetition that is not an email). Fallback to PID-14.
- **Email**: Scans PID-13 repetitions for `@` in component 4 or component 1 with telecom type `Internet`/`NET`.

### Database Tables

| Table | Purpose |
|-------|---------|
| `patients` | +`phone_number`, +`email` columns |
| `hl7_messages` | +`patient_phone`, +`patient_email`, +`patient_sex`, +`patient_birth_date` |
| `whatsapp_templates` | Template definitions with ContentSid |
| `whatsapp_template_variables` | Ordered positional variables per template |
| `whatsapp_auto_send_rules` | StudyStatus → Template mapping (unique per status) |
| `whatsapp_notifications` | Immutable send audit trail per recipient |

### Encryption

Provider credentials (`whatsapp.provider_config`) are encrypted at rest using **ASP.NET Core Data Protection**:

- Automatic key rotation (90-day cycle)
- No manual key/IV management
- Encrypted values prefixed with `ENC:` for identification
- Implemented via `ISettingEncryptionService` in `Dicom.Edge.Security`

---

## 🛡️ Security

- JWT authentication via `Dicom.Edge.Security`
- ASP.NET Core Data Protection for encrypted settings
- PHI redaction in audit logs (phone numbers: `+521****5678`)
- Role-based access control for API endpoints

## 🔧 Tech Stack

- .NET 10, C# 14
- ASP.NET Core
- Entity Framework Core + PostgreSQL
- Twilio SDK (WhatsApp delivery)
- fo-dicom (DICOM protocol)
- Angular (Frontend)
