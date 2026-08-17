# HL7 Integration Guide

This guide is for HIS/RIS integration teams connecting to the EdgeGuard Platform Hub.

---

## Table of Contents

1. [Connection Information](#connection-information)
2. [MLLP Framing Requirements](#mllp-framing-requirements)
3. [Supported Message Types](#supported-message-types)
4. [Message Examples](#message-examples)
   - [ADT^A01 — Patient Admit/Register](#adta01--patient-admitregister)
   - [ADT^A40 — Patient Merge](#adta40--patient-merge)
   - [ORM^O01 — Order Message](#ormo01--order-message)
   - [ORU^R01 — Observation Result](#orur01--observation-result)
5. [Required vs Optional Fields](#required-vs-optional-fields)
6. [ACK Response Format](#ack-response-format)
7. [Error Handling (NACK)](#error-handling-nack)
8. [Common Mistakes](#common-mistakes)
9. [Test Procedure](#test-procedure)
10. [Support Contact](#support-contact)

---

## Connection Information

| Parameter | Value |
|-----------|-------|
| Protocol | TCP/IP with MLLP framing |
| Host | Hub server IP or hostname |
| Port | **8001** |
| Character encoding | UTF-8 |
| HL7 version | 2.5 |

> **Important:** The connection uses MLLP (Minimal Lower Layer Protocol) framing. Raw TCP without MLLP framing will not be processed. See [MLLP Framing Requirements](#mllp-framing-requirements).

---

## MLLP Framing Requirements

All HL7 messages must be wrapped in MLLP frames:

```
[0x0B] <HL7 message text> [0x1C][0x0D]
```

| Byte | Hex | Name | Position |
|------|-----|------|----------|
| Vertical Tab | `0x0B` | Start Block (SB) | Before message |
| File Separator | `0x1C` | End Block (EB) | After message |
| Carriage Return | `0x0D` | Carriage Return (CR) | After End Block |

**Example frame (pseudocode):**
```
\x0B MSH|^~\&|HIS|SITE|... <rest of message> \x1C\x0D
```

Segment separators within the message are `\r` (carriage return, `0x0D`), as per HL7 2.x specification.

---

## Supported Message Types

| Message Type | Trigger Event | Purpose |
|-------------|---------------|---------|
| ADT^A01 | Admit / Register Patient | Creates or updates a patient record; notifies of scheduled appointment |
| ADT^A40 | Merge Patient Records | Merges two patient identifiers into one (MRN correction) |
| ORM^O01 | Imaging Order | Creates a scheduled procedure and populates Modality Worklist |
| ORU^R01 | Observation Result | Attaches image links or report results to a study |

---

## Message Examples

### ADT^A01 — Patient Admit/Register

```hl7
MSH|^~\&|HIS_SYSTEM|YOUR_FACILITY|EDGEGUARD|EDGE|20250115083000||ADT^A01|MSG001|P|2.5|||AL|NE|
EVN|A01|20250115083000|||
PID|1||MRN123456^^^YOUR_FACILITY^MR||Smith^John^A||19800115|M|||123 Main St^^Springfield^IL^62701^USA|||||||ACC20250115001|
PV1|1|I|RAD^101^A^YOUR_FACILITY||||1234567^Jones^Sarah^M^MD|||RAD||||||||VN12345|
```

**Annotated fields:**

| Segment | Field | Value | Description |
|---------|-------|-------|-------------|
| MSH | MSH-3 | `HIS_SYSTEM` | Sending application |
| MSH | MSH-4 | `YOUR_FACILITY` | Sending facility |
| MSH | MSH-9 | `ADT^A01` | Message type and trigger |
| MSH | MSH-10 | `MSG001` | Unique message control ID |
| PID | PID-3 | `MRN123456^^^YOUR_FACILITY^MR` | Patient ID (MRN) — **required** |
| PID | PID-5 | `Smith^John^A` | Patient name (Last^First^MI) |
| PID | PID-7 | `19800115` | Date of birth (YYYYMMDD) |
| PID | PID-8 | `M` | Sex (M/F/U) |
| PV1 | PV1-8 | `1234567^Jones^Sarah^M^MD` | Referring physician |

---

### ADT^A40 — Patient Merge

Merges `OLD_MRN` into `NEW_MRN`. After processing, all studies associated with `OLD_MRN` are reassigned to `NEW_MRN`.

```hl7
MSH|^~\&|HIS_SYSTEM|YOUR_FACILITY|EDGEGUARD|EDGE|20250115090000||ADT^A40|MSG002|P|2.5|||AL|NE|
EVN|A40|20250115090000|||
PID|1||NEW_MRN789^^^YOUR_FACILITY^MR||Doe^Jane^B||19751020|F|||
MRG|OLD_MRN123^^^YOUR_FACILITY^MR|
```

**Annotated fields:**

| Segment | Field | Value | Description |
|---------|-------|-------|-------------|
| PID | PID-3 | `NEW_MRN789` | The surviving/correct patient ID |
| MRG | MRG-1 | `OLD_MRN123` | The prior/incorrect patient ID to merge from |

> **Critical:** The `MRG` segment is required. Without it, the merge is rejected with NACK.

---

### ORM^O01 — Order Message

Creates an imaging order and populates the Modality Worklist.

```hl7
MSH|^~\&|RIS_SYSTEM|YOUR_FACILITY|EDGEGUARD|EDGE|20250115090000||ORM^O01|MSG003|P|2.5|||AL|NE|
PID|1||MRN123456^^^YOUR_FACILITY^MR||Smith^John^A||19800115|M|||
PV1|1|O|RAD^101^A^YOUR_FACILITY||||1234567^Jones^Sarah^M^MD|||RAD|
ORC|NW|ORD001^RIS_SYSTEM|||||^^^20250115^^R||20250115090000|||1234567^Jones^Sarah^M^MD|
OBR|1|ORD001^RIS_SYSTEM||71046^Chest X-Ray 2 views^CPT4|||20250115|||||||||||ACC20250115002||||||CT||^^^20250115090000^^R|
ZDS|1.2.840.10008.5.1.4.1.1.2.20250115001^^^INSTANCE|
```

**Annotated fields:**

| Segment | Field | Value | Description |
|---------|-------|-------|-------------|
| ORC | ORC-1 | `NW` | Order control: New order |
| ORC | ORC-2 | `ORD001^RIS_SYSTEM` | Placer order number |
| OBR | OBR-4 | `71046^Chest X-Ray 2 views^CPT4` | Universal Service ID (procedure) |
| OBR | OBR-36 | `20250115090000` | Scheduled date/time |
| OBR | OBR-18 | `ACC20250115002` | Accession number |
| ZDS | ZDS-1 | `1.2.840...` | Study Instance UID (local extension) |

---

### ORU^R01 — Observation Result

Attaches image links or report text to an existing study.

```hl7
MSH|^~\&|PACS_SYSTEM|YOUR_FACILITY|EDGEGUARD|EDGE|20250115150000||ORU^R01|MSG004|P|2.5|||AL|NE|
PID|1||MRN123456^^^YOUR_FACILITY^MR||Smith^John^A||19800115|M|||
OBR|1|ORD001^RIS_SYSTEM|ACC20250115002|71046^Chest X-Ray 2 views^CPT4|||20250115140000|||||||||||ACC20250115002|
OBX|1|RP|18726-0^Radiology study URL^LN||https://pacs.internal.example.com/viewer/1.2.840.10008.5.1.4.1.1.2.001||||||F|
OBX|2|TX|59380-7^Radiology Report^LN||Normal chest X-ray. No acute cardiopulmonary process.||||||F|
```

**OBX Value Types for image links:**

| OBX-2 Type | Use |
|------------|-----|
| `RP` | Reference Pointer — URL to image viewer (preferred) |
| `ED` | Encapsulated Data — base64 encoded data |
| `TX` | Text — report text (not an image link) |

---

## Required vs Optional Fields

### ADT^A01

| Segment | Field | Required | Notes |
|---------|-------|----------|-------|
| MSH | MSH-9 | Yes | Must be `ADT^A01` |
| MSH | MSH-10 | Yes | Unique message control ID |
| PID | PID-3 | Yes | Patient ID (MRN), CX format |
| PID | PID-5 | Yes | Patient name |
| PID | PID-7 | Recommended | Date of birth |
| PID | PID-8 | Recommended | Sex |
| PV1 | PV1-8 | Optional | Referring physician |

### ADT^A40

| Segment | Field | Required | Notes |
|---------|-------|----------|-------|
| MSH | MSH-9 | Yes | Must be `ADT^A40` |
| PID | PID-3 | Yes | New (surviving) patient ID |
| MRG | MRG-1 | Yes | Prior (old) patient ID — merge fails without this |

### ORM^O01

| Segment | Field | Required | Notes |
|---------|-------|----------|-------|
| PID | PID-3 | Yes | Patient ID |
| ORC | ORC-1 | Yes | Order control (`NW` for new) |
| ORC | ORC-2 | Yes | Placer order number |
| OBR | OBR-4 | Yes | Procedure code |
| OBR | OBR-36 | Yes | Scheduled date/time |
| OBR | OBR-18 | Recommended | Accession number |

### ORU^R01

| Segment | Field | Required | Notes |
|---------|-------|----------|-------|
| PID | PID-3 | Yes | Patient ID |
| OBR | OBR-18 | Yes | Accession number (used to match study) |
| OBX | OBX-2 | Yes | Value type (`RP`, `ED`, or `TX`) |
| OBX | OBX-5 | Yes | Observation value |

---

## ACK Response Format

The Hub responds to every valid MLLP message with an ACK:

```hl7
MSH|^~\&|EDGEGUARD|EDGE|HIS_SYSTEM|YOUR_FACILITY|20250115090001||ACK^A01|ACK001|P|2.5
MSA|AA|MSG001|Message processed successfully|
```

| MSA-1 Code | Meaning |
|------------|---------|
| `AA` | Application Accept — message processed successfully |
| `AE` | Application Error — message rejected (see MSA-3 for reason) |
| `AR` | Application Reject — message structurally invalid |

---

## Error Handling (NACK)

When validation fails, the Hub returns a NACK with `MSA-1 = AE` and the error description in `MSA-3`:

```hl7
MSH|^~\&|EDGEGUARD|EDGE|HIS_SYSTEM|YOUR_FACILITY|20250115090001||ACK^A01|NACK001|P|2.5
MSA|AE|MSG001|Validation failed: PID-3 (Patient ID) is required and missing|
ERR||PID^1^3^1|102|E|||Required field missing: Patient Identifier List (PID-3)|
```

**Integration guidance for NACK handling:**

1. Parse `MSA-1` on every ACK response.
2. If `MSA-1 = AE` or `AE`, log `MSA-3` and `ERR` segment details for investigation.
3. Do not retry a NACK'd message without fixing the message content — the same message will be rejected again.
4. Alert on NACK rate exceeding 1% of messages.

---

## Common Mistakes

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Missing MLLP framing | Connection timeout or no response | Wrap message with `0x0B` ... `0x1C 0x0D` |
| Wrong trigger event | NACK: Unsupported trigger event | Use only A01, A40, O01, R01 |
| Missing PID-3 | NACK: Required field missing | Always include patient MRN in PID-3 |
| Missing MRG segment in A40 | Merge silently ignored | Include MRG segment with prior patient ID |
| OBX value type not RP/ED | Image link not attached | Set OBX-2 to `RP` for image URL references |
| Segment delimiter wrong | Parse error | Use `\r` (0x0D) between segments, not `\n` (0x0A) |
| Wrong HL7 version in MSH-12 | Schema validation error | Use `2.5` |
| MSH-10 not unique | Duplicate detection triggers | Use UUID or timestamp-based message control ID |

---

## Test Procedure

### Using HAPI TestPanel (free, cross-platform)

1. Download HAPI TestPanel from: https://hapifhir.github.io/hapi-hl7v2/hapi-testpanel/
2. Launch the application.
3. Create a new connection:
   - **Host:** `<hub-ip-or-hostname>`
   - **Port:** `8001`
   - **Protocol:** MLLP
4. Open the **Messages** tab and paste one of the example messages above.
5. Click **Send** and observe the ACK in the response panel.
6. Verify in the Hub UI that the patient/study was created.

### Test message checklist

- [ ] ADT^A01: Patient created in Hub → visible in Patients list
- [ ] ORM^O01: Study created with status `Scheduled` → visible in Worklist
- [ ] ORU^R01: Image link attached to study → viewer link appears in Study detail
- [ ] ADT^A40: Duplicate patient merged → old MRN studies reassigned
- [ ] Invalid message: NACK returned with descriptive error

---

## Support Contact

For integration issues:

| Item | Details |
|------|---------|
| **Integration Lead** | [Contact your EdgeGuard implementation team] |
| **Test environment** | Confirm with implementation team |
| **Log access** | Hub UI → System → HL7 Message Log (requires Admin role) |
| **Escalation** | [Your organization's support channel] |

Please include the following when reporting issues:

1. Message Control ID (MSH-10) of the failing message.
2. The full NACK response received (MSA-3 and ERR segment).
3. Timestamp and sending system name.
4. Sample of the raw HL7 message (with PHI removed from PID segment).
