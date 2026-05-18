# Database Schema

EdgeGuard Platform uses two database engines: **PostgreSQL** on the Hub (centralised, relational, multi-user) and **SQLite** on each Edge Node (embedded, offline-first, single-writer). Both are managed via EF Core with code-first migrations.

---

## Hub — PostgreSQL

**Database name:** `edgeguard_hub`
**Schema:** All tables reside in the default `public` schema.
**Managed by:** `Dicom.Edge.Hub.Persistence` — EF Core migrations.

---

### Table: `studies`

Stores each DICOM study received from an Edge Node.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `id` | `uuid` | NOT NULL | Primary key |
| `study_instance_uid` | `varchar(64)` | NOT NULL | DICOM Study Instance UID (value object validated on write) |
| `accession_number` | `varchar(64)` | NULL | Accession number from modality or HIS |
| `status` | `varchar(32)` | NOT NULL | `Scheduled \| Receiving \| Received \| Sending \| Completed \| Failed` |
| `patient_id` | `uuid` | NULL | FK → `patients.id` |
| `patient_name` | `varchar(256)` | NULL | Denormalised; updated on patient merge |
| `source_node_id` | `uuid` | NULL | FK → `nodes.id` |
| `source_ae_title` | `varchar(16)` | NULL | DICOM AE title of the sending modality |
| `receiving_ae_title` | `varchar(16)` | NULL | DICOM AE title of the Node SCP |
| `instance_count` | `integer` | NOT NULL | Default 0; incremented as instances arrive |
| `series_count` | `integer` | NOT NULL | Default 0 |
| `total_size_bytes` | `bigint` | NOT NULL | Default 0 |
| `target_pacs_id` | `uuid` | NULL | FK → `pacs_servers.id`; resolved by routing |
| `external_image_links` | `jsonb` | NULL | Array of viewer URL strings |
| `priority` | `integer` | NOT NULL | Default 0; higher = higher priority |
| `is_urgent` | `boolean` | NOT NULL | Default false |
| `retry_count` | `integer` | NOT NULL | Default 0; incremented on send failure |
| `is_deleted` | `boolean` | NOT NULL | Soft delete flag |
| `created_at` | `timestamp with time zone` | NOT NULL | Set on insert |
| `updated_at` | `timestamp with time zone` | NOT NULL | Updated on every mutation |

**Indexes:**

| Index Name | Columns | Type | Notes |
|---|---|---|---|
| `ix_studies_is_deleted_created_at` | `is_deleted`, `created_at DESC` | BTREE | Primary list query filter |
| `ix_studies_study_instance_uid` | `study_instance_uid` | UNIQUE | Prevents duplicate studies |
| `ix_studies_patient_id` | `patient_id` | BTREE | Patient → studies join |
| `ix_studies_source_node_id` | `source_node_id` | BTREE | Node → studies filter |
| `ix_studies_status` | `status` | BTREE | Status-based queue queries |

---

### Table: `patients`

Stores patient demographics. Supports HL7 A40 patient merge.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `id` | `uuid` | NOT NULL | Primary key |
| `patient_dicom_id` | `varchar(64)` | NOT NULL | DICOM Patient ID value object |
| `patient_name` | `varchar(256)` | NULL | DICOM PN format |
| `birth_date` | `date` | NULL | |
| `sex` | `varchar(1)` | NULL | `M \| F \| O \| U` |
| `phone_number` | `varchar(32)` | NULL | |
| `email` | `varchar(256)` | NULL | |
| `issuer_of_patient_id` | `varchar(64)` | NULL | Identifies patient ID namespace |
| `merged_into_patient_id` | `uuid` | NULL | FK → `patients.id`; set on A40 merge |
| `is_active` | `boolean` | NOT NULL | False after merge |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `created_at` | `timestamp with time zone` | NOT NULL | |
| `updated_at` | `timestamp with time zone` | NOT NULL | |

**Indexes:**

| Index Name | Columns | Type | Notes |
|---|---|---|---|
| `ix_patients_patient_dicom_id` | `patient_dicom_id` | UNIQUE (partial: `is_deleted = false`) | Prevents duplicate active patients |
| `ix_patients_merged_into` | `merged_into_patient_id` | BTREE (partial: `merged_into_patient_id IS NOT NULL`) | Merge chain traversal |

---

### Table: `hl7_messages`

Stores every inbound HL7 v2 message received by the TCP listener, in both raw and parsed form.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `id` | `uuid` | NOT NULL | Primary key |
| `content` | `text` | NOT NULL | Raw HL7 MLLP message body |
| `message_type` | `varchar(8)` | NOT NULL | `ADT \| ORM \| ORU` |
| `trigger_event` | `varchar(8)` | NOT NULL | e.g. `A01`, `A08`, `A40`, `O01` |
| `status` | `varchar(32)` | NOT NULL | `Pending \| Processed \| Failed \| Skipped` |
| `sending_application` | `varchar(64)` | NULL | MSH-3 |
| `sending_facility` | `varchar(64)` | NULL | MSH-4 |
| `patient_id` | `varchar(64)` | NULL | PID-3 parsed value |
| `patient_name` | `varchar(256)` | NULL | PID-5 parsed value |
| `accession_number` | `varchar(64)` | NULL | OBR-18 or ZDS segment |
| `mrg_prior_patient_id` | `varchar(64)` | NULL | MRG-1; A40 merge source |
| `mrg_prior_patient_name` | `varchar(256)` | NULL | MRG-7 |
| `mrg_prior_accession_number` | `varchar(64)` | NULL | Prior accession on merge |
| `image_links_json` | `text` | NULL | JSON array of image viewer URLs (OBX segments) |
| `error_message` | `text` | NULL | Set when `status = Failed` |
| `received_at` | `timestamp with time zone` | NOT NULL | Timestamp of TCP receipt |
| `processed_at` | `timestamp with time zone` | NULL | Timestamp of successful processing |

**Indexes:**

| Index Name | Columns | Type | Notes |
|---|---|---|---|
| `ix_hl7_messages_dispatch_queue` | `status`, `received_at` | BTREE (partial: `status = 'Pending'`) | Efficient pending-message queue scan |
| `ix_hl7_messages_patient_id` | `patient_id` | BTREE | Patient message history lookup |
| `ix_hl7_messages_accession_number` | `accession_number` | BTREE | Accession-based lookup |
| `ix_hl7_messages_received_at` | `received_at DESC` | BTREE | Chronological listing |

---

### Table: `node_dicom_routing_rules`

Per-node DICOM routing rule definitions, managed by the Hub and pushed to nodes on change.

| Column | Type | Nullable | Notes |
|---|---|---|---|
| `id` | `uuid` | NOT NULL | Primary key |
| `node_id` | `uuid` | NOT NULL | FK → `nodes.id` |
| `name` | `varchar(128)` | NOT NULL | Human-readable rule name |
| `priority` | `integer` | NOT NULL | Lower number = evaluated first |
| `is_enabled` | `boolean` | NOT NULL | Disabled rules are skipped |
| `match_modality` | `varchar(16)` | NULL | NULL = match all modalities |
| `match_source_ae_title` | `varchar(16)` | NULL | NULL = match all AE titles |
| `match_institution` | `varchar(64)` | NULL | DICOM Institution Name tag match |
| `match_study_desc` | `varchar(64)` | NULL | Substring match on Study Description |
| `min_instance_count` | `integer` | NULL | NULL = no minimum |
| `max_instance_count` | `integer` | NULL | NULL = no maximum |
| `destination_ae_title` | `varchar(16)` | NULL | Target AE title (if SendToPacs) |
| `send_to_pacs` | `boolean` | NOT NULL | Forward to PACS destination |
| `send_to_hub` | `boolean` | NOT NULL | Report to Hub |
| `anonymize_before_send` | `boolean` | NOT NULL | Apply anonymisation before forwarding |
| `match_count` | `bigint` | NOT NULL | Lifetime match counter |
| `last_matched_at` | `timestamp with time zone` | NULL | Last time rule matched a study |

---

### Other Hub Tables (Summary)

| Table | Key Columns | Purpose |
|---|---|---|
| `nodes` | `id`, `name`, `ae_title`, `host`, `port`, `token_hash`, `is_online`, `last_seen_at` | Registered Edge Node registry |
| `pacs_servers` | `id`, `name`, `ae_title`, `host`, `port`, `is_active` | PACS destination definitions |
| `routing_rules` | `id`, `name`, `priority`, `is_enabled`, conditions, `action` | Hub-level routing rules |
| `audit_logs` | `id`, `action`, `entity_type`, `entity_id`, `actor_id`, `before_json`, `after_json`, `timestamp` | Immutable audit trail |
| `system_settings` | `key`, `value`, `data_type`, `updated_at` | Key-value Hub configuration |
| `users` | `id`, `email`, `password_hash`, `roles_json`, `refresh_token_hash`, `is_active` | Operator accounts |

---

## Node — SQLite

**Database file:** `edgeguard_node.db` (path configurable in `appsettings.json`)
**Managed by:** `Dicom.Edge.Node.Persistence` — EF Core migrations.

The Node database is intentionally simple. It stores only what the node needs to operate autonomously.

### Table: `node_studies`

| Column | Type | Notes |
|---|---|---|
| `id` | TEXT (UUID) | Primary key |
| `study_instance_uid` | TEXT | DICOM Study Instance UID |
| `accession_number` | TEXT | |
| `patient_name` | TEXT | |
| `modality` | TEXT | |
| `status` | TEXT | `Received \| Sending \| Sent \| Failed` |
| `instance_count` | INTEGER | |
| `total_size_bytes` | INTEGER | |
| `source_ae_title` | TEXT | |
| `retry_count` | INTEGER | |
| `hub_reported` | INTEGER | 0 = not reported to Hub; 1 = reported |
| `received_at` | TEXT | ISO-8601 UTC |
| `sent_at` | TEXT | NULL until successfully forwarded |

### Table: `node_pacs_servers`

| Column | Type | Notes |
|---|---|---|
| `id` | TEXT (UUID) | Primary key (Hub-assigned) |
| `name` | TEXT | |
| `ae_title` | TEXT | |
| `host` | TEXT | |
| `port` | INTEGER | |
| `is_active` | INTEGER | 0 \| 1 |
| `synced_at` | TEXT | Last Hub sync timestamp |

### Table: `node_routing_rules`

| Column | Type | Notes |
|---|---|---|
| `id` | TEXT (UUID) | Primary key (Hub-assigned) |
| `name` | TEXT | |
| `priority` | INTEGER | |
| `is_enabled` | INTEGER | |
| `match_modality` | TEXT | NULL = any |
| `match_source_ae_title` | TEXT | NULL = any |
| `match_institution` | TEXT | NULL = any |
| `match_study_desc` | TEXT | NULL = any |
| `min_instance_count` | INTEGER | NULL = no min |
| `max_instance_count` | INTEGER | NULL = no max |
| `destination_ae_title` | TEXT | |
| `send_to_pacs` | INTEGER | |
| `send_to_hub` | INTEGER | |
| `anonymize_before_send` | INTEGER | |
| `synced_at` | TEXT | |

### Table: `node_settings`

| Column | Type | Notes |
|---|---|---|
| `key` | TEXT | Primary key |
| `value` | TEXT | |
| `updated_at` | TEXT | |

Key entries: `AeTitle`, `ListenPort`, `HubUrl`, `HubNodeToken`, `MaxStorageMb`, `WorklistEnabled`.

---

## EF Core Migration History

Migrations are applied automatically at application startup (`MigrateAsync()` in `Program.cs`).

### Hub Migrations (Chronological)

| Migration Name | Description |
|---|---|
| `InitialCreate` | Create `studies`, `patients`, `nodes`, `pacs_servers`, `users`, `system_settings` |
| `AddAuditLogs` | Add `audit_logs` table |
| `AddHl7Messages` | Add `hl7_messages` table with full parsed columns |
| `AddNodeDicomRoutingRules` | Add `node_dicom_routing_rules` with match and action columns |
| `AddRoutingRules` | Add Hub-level `routing_rules` table |
| `AddStudyExternalImageLinks` | Add `external_image_links` JSONB column to `studies` |
| `AddStudyPriorityAndUrgency` | Add `priority`, `is_urgent` columns to `studies` |
| `AddStudyRetryCount` | Add `retry_count` column to `studies` |
| `AddPatientMergeSupport` | Add `merged_into_patient_id`, `issuer_of_patient_id` to `patients` |
| `AddRoutingRuleMatchStats` | Add `match_count`, `last_matched_at` to `node_dicom_routing_rules` |
| `AddNodeOnlineStatus` | Add `is_online`, `last_seen_at` to `nodes` |

### Node Migrations (Chronological)

| Migration Name | Description |
|---|---|
| `InitialNodeCreate` | Create `node_studies`, `node_pacs_servers`, `node_routing_rules`, `node_settings` |
| `AddNodeStudyHubReported` | Add `hub_reported` column to `node_studies` |
| `AddNodeStudySentAt` | Add `sent_at` column to `node_studies` |
| `AddRoutingRuleAnonymize` | Add `anonymize_before_send` column to `node_routing_rules` |

> **Note:** Migration names follow the pattern `<Description>` without timestamps, relying on EF Core's `__EFMigrationsHistory` table to track applied migrations. Always run `dotnet ef migrations add <Name>` from the appropriate Persistence project directory using the correct startup project flag.
