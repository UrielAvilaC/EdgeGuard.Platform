# DICOM Conformance Statement

**Product:** EdgeGuard Platform  
**Version:** 1.5.x  
**Document revision:** 2025-01-15  
**Standard:** DICOM PS3 2024a

---

> **Scope:** This is an abbreviated conformance statement suitable for integration planning. For the full formal conformance statement required by IEC 62119, contact your EdgeGuard implementation team.

---

## Table of Contents

1. [Implementation Identification](#implementation-identification)
2. [SCP Services (Server — Receiving)](#scp-services-server--receiving)
3. [SCU Services (Client — Sending)](#scu-services-client--sending)
4. [Transfer Syntaxes Supported](#transfer-syntaxes-supported)
5. [AE Title Configuration](#ae-title-configuration)
6. [Association Parameters](#association-parameters)
7. [Security Profiles](#security-profiles)
8. [Tested Modalities](#tested-modalities)
9. [SOP Classes — Storage](#sop-classes--storage)

---

## Implementation Identification

| Item | Value |
|------|-------|
| Implementation Class UID | 1.2.826.0.1.3680043.10.xxx (site-assigned) |
| Implementation Version Name | `EDGEGUARD_1_5` |
| Product name | EdgeGuard Platform |
| Manufacturer | [Your organization] |
| DICOM standard version | PS3 2024a |

---

## SCP Services (Server — Receiving)

The Edge Node acts as a DICOM Service Class Provider (SCP) — it receives DICOM objects and association requests from modalities and other DICOM devices.

### C-STORE SCP

Accepts DICOM storage requests from imaging modalities.

- **Role:** Service Class Provider (SCP)
- **Trigger:** Modality or DICOM device initiates C-STORE
- **Behavior:** Stores DICOM file temporarily in `./data/`; creates or updates Study record; forwards to configured PACS

### C-FIND SCP — Modality Worklist (MWL)

Provides Modality Worklist responses to modalities querying for scheduled procedures.

- **Role:** SCP
- **SOP Class:** Modality Worklist Information Model — FIND (1.2.840.10008.5.1.4.31)
- **Supported query keys:**

| Attribute | Tag | Query Type |
|-----------|-----|-----------|
| Scheduled Procedure Step Start Date | (0040,0100)\>(0040,0001) | Range / Matching |
| Modality | (0008,0060) | Single Value |
| Scheduled Station AE Title | (0040,0001) | Single Value |
| Accession Number | (0008,0050) | Single Value / Wildcard |
| Requesting Physician | (0032,1032) | Wildcard |
| Patient ID | (0010,0020) | Single Value / Wildcard |
| Patient Name | (0010,0010) | Wildcard |

### C-FIND SCP — Study Root

Enables DICOM Query/Retrieve (Q/R) of studies stored or tracked by EdgeGuard.

- **Role:** SCP
- **SOP Class:** Study Root Query/Retrieve Information Model — FIND (1.2.840.10008.5.1.4.1.2.2.1)
- **Supported levels:** Study, Series, Image

### C-ECHO SCP — Verification

Responds to DICOM Verification (ping) requests.

- **Role:** SCP
- **SOP Class:** Verification SOP Class (1.2.840.10008.1.1)
- **Behavior:** Always responds with Success (0000H) if association is accepted

---

## SCU Services (Client — Sending)

The Edge Node (and Hub) act as DICOM Service Class User (SCU) when sending studies to PACS systems.

### C-STORE SCU

Sends DICOM studies to configured PACS destinations.

- **Role:** Service Class User (SCU)
- **Trigger:** Study received and routing rules determine target PACS; manual retry
- **Behavior:** Initiates association to PACS, sends all SOP Instances in the study, releases association

### C-ECHO SCU

Tests DICOM connectivity to PACS servers.

- **Role:** SCU
- **Trigger:** Manual test via Hub UI or API
- **Behavior:** Initiates association, sends C-ECHO RQ, logs result

---

## Transfer Syntaxes Supported

### Accepted (SCP — receiving)

| Transfer Syntax | UID |
|----------------|-----|
| Implicit VR Little Endian | 1.2.840.10008.1.2 |
| Explicit VR Little Endian | 1.2.840.10008.1.2.1 |
| Explicit VR Big Endian (retired, compat) | 1.2.840.10008.1.2.2 |
| JPEG Baseline (Process 1) | 1.2.840.10008.1.2.4.50 |
| JPEG Extended (Process 2 & 4) | 1.2.840.10008.1.2.4.51 |
| JPEG Lossless, Non-hierarchical (Process 14) | 1.2.840.10008.1.2.4.57 |
| JPEG-LS Lossless | 1.2.840.10008.1.2.4.80 |
| JPEG-LS Near-Lossless | 1.2.840.10008.1.2.4.81 |
| JPEG 2000 Lossless Only | 1.2.840.10008.1.2.4.90 |
| JPEG 2000 | 1.2.840.10008.1.2.4.91 |
| MPEG-4 AVC/H.264 High Profile | 1.2.840.10008.1.2.4.102 |
| MPEG-4 AVC/H.264 BD-Compatible | 1.2.840.10008.1.2.4.103 |
| Deflated Explicit VR Little Endian | 1.2.840.10008.1.2.1.99 |

### Proposed (SCU — sending)

When sending to PACS, EdgeGuard proposes the following transfer syntaxes in preference order:

1. Explicit VR Little Endian (always proposed first)
2. Implicit VR Little Endian
3. Original transfer syntax of the received object (if different from above)

---

## AE Title Configuration

The EdgeGuard AE Title is configurable per deployment.

| Component | AE Title setting | Location |
|-----------|-----------------|----------|
| Edge Node | `Dicom.AeTitle` in `appsettings.json` | Node configuration |
| Hub (if acting as SCP) | `Dicom.HubAeTitle` in Hub `appsettings.json` | Hub configuration |

**AE Title constraints:**
- Maximum 16 characters
- Uppercase ASCII letters, digits, and underscore only
- No leading or trailing spaces
- Must be unique within your DICOM network

**Example AE Titles:**
```
EDGEGUARD_SITE_A
EDGEGUARD_RADIOL
EG_NODE_01
```

---

## Association Parameters

### SCP (receiving) defaults

| Parameter | Value | Configurable |
|-----------|-------|-------------|
| Maximum PDU size | 131072 bytes (128 KB) | Yes — `Dicom.MaxPduSize` |
| Maximum concurrent associations | 10 | Yes — `Dicom.MaxConcurrentConnections` |
| Association accept timeout | 30 seconds | Yes |
| DIMSE timeout | 60 seconds | Yes |
| Calling AE restriction | None (accept all) | Yes — configurable allowlist |

### SCU (sending) defaults

| Parameter | Value | Configurable |
|-----------|-------|-------------|
| Maximum PDU size | 131072 bytes (128 KB) | Yes |
| Association request timeout | 30 seconds | Yes |
| DIMSE command timeout | 120 seconds | Yes |
| Retry on failure | 3 attempts | Yes — `Dicom.PacsSend.MaxRetryAttempts` |
| Retry interval | 60 seconds | Yes |

---

## Security Profiles

### Network security

| Profile | Support |
|---------|---------|
| DICOM TLS — Basic TLS Secure Transport Connection Profile | Supported (optional) |
| DICOM TLS — ISCL Secure Transport Connection Profile | Not supported |
| User Authentication — Kerberos | Not applicable |
| Audit Trail Message Format | Partial (structured logs, not ATNA) |

### TLS configuration

When enabled, EdgeGuard uses:
- TLS 1.2 minimum
- TLS 1.3 preferred
- Certificate-based mutual authentication (optional)

See [pacs-integration.md — Secure DICOM](./pacs-integration.md#secure-dicom-tls) for configuration details.

### Access control

Calling AE Title restriction is configurable. When enabled, the SCP only accepts associations from AE Titles in the configured allowlist. Unrecognized AE Titles receive Association Reject: `Calling AE Title Not Recognized`.

---

## Tested Modalities

EdgeGuard has been tested with DICOM C-STORE from the following modality types:

| Modality Code | Modality Type | Notes |
|---------------|--------------|-------|
| CT | Computed Tomography | All common CT manufacturers |
| MR | Magnetic Resonance | All standard MR Storage SOP classes |
| CR | Computed Radiography | Single and multi-frame |
| DX | Digital Radiography | Including presentation states |
| US | Ultrasound | Multi-frame US Image Storage |
| PT | Positron Emission Tomography | PET Image Storage |
| NM | Nuclear Medicine | NM Image Storage |
| XA | X-Ray Angiography | XA Image Storage |
| RF | Radio Fluoroscopy | RF Image Storage |
| SC | Secondary Capture | SC Image Storage |

---

## SOP Classes — Storage

EdgeGuard accepts all standard Storage SOP Classes defined in DICOM PS3.4 Annex B. The following table lists the most commonly encountered classes:

| SOP Class Name | SOP Class UID |
|----------------|---------------|
| CT Image Storage | 1.2.840.10008.5.1.4.1.1.2 |
| Enhanced CT Image Storage | 1.2.840.10008.5.1.4.1.1.2.1 |
| MR Image Storage | 1.2.840.10008.5.1.4.1.1.4 |
| Enhanced MR Image Storage | 1.2.840.10008.5.1.4.1.1.4.1 |
| CR Image Storage | 1.2.840.10008.5.1.4.1.1.1 |
| Digital X-Ray Image Storage — For Presentation | 1.2.840.10008.5.1.4.1.1.1.1 |
| Digital X-Ray Image Storage — For Processing | 1.2.840.10008.5.1.4.1.1.1.1.1 |
| Ultrasound Image Storage | 1.2.840.10008.5.1.4.1.1.6.1 |
| Ultrasound Multi-frame Image Storage | 1.2.840.10008.5.1.4.1.1.3.1 |
| Nuclear Medicine Image Storage | 1.2.840.10008.5.1.4.1.1.20 |
| Positron Emission Tomography Image Storage | 1.2.840.10008.5.1.4.1.1.128 |
| X-Ray Angiographic Image Storage | 1.2.840.10008.5.1.4.1.1.12.1 |
| X-Ray Radiofluoroscopy Image Storage | 1.2.840.10008.5.1.4.1.1.12.2 |
| Secondary Capture Image Storage | 1.2.840.10008.5.1.4.1.1.7 |
| Multi-frame Secondary Capture Image Storage | 1.2.840.10008.5.1.4.1.1.7.2 |
| Grayscale Softcopy Presentation State Storage | 1.2.840.10008.5.1.4.1.1.11.1 |
| Color Softcopy Presentation State Storage | 1.2.840.10008.5.1.4.1.1.11.2 |
| Structured Report Document Storage | 1.2.840.10008.5.1.4.1.1.88.11 |
| Encapsulated PDF Storage | 1.2.840.10008.5.1.4.1.1.104.1 |
| Verification SOP Class | 1.2.840.10008.1.1 |
| Modality Worklist Info Model — FIND | 1.2.840.10008.5.1.4.31 |
| Study Root Q/R Info Model — FIND | 1.2.840.10008.5.1.4.1.2.2.1 |

> **Note:** For SOP Classes not listed above, EdgeGuard will attempt to accept the association. If the SOP Class is not recognized, it is stored and forwarded as-is (unknown SOP class handling). Contact the implementation team if specific SOP Class support needs to be verified.
