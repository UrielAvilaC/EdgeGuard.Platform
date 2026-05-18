# Monitoring and Observability

EdgeGuard Platform uses Serilog for structured logging, with optional sinks for Seq and OpenTelemetry. Health check endpoints provide real-time operational status.

---

## Table of Contents

1. [Health Check Endpoints](#health-check-endpoints)
2. [Log Configuration](#log-configuration)
3. [PHI Redaction](#phi-redaction)
4. [Correlation ID Propagation](#correlation-id-propagation)
5. [OpenTelemetry and Prometheus](#opentelemetry-and-prometheus)
6. [Key Metrics to Monitor](#key-metrics-to-monitor)
7. [Log Examples](#log-examples)

---

## Health Check Endpoints

Both the Hub and Node expose health endpoints. These are suitable for load balancer probes and uptime monitors.

### GET /health

Returns the overall health status and individual check results.

**Request:**
```
GET /health HTTP/1.1
Host: your-hub-domain.example.com
```

**Response — Healthy (200 OK):**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0423815",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "PostgreSQL connection established",
      "duration": "00:00:00.0310000"
    },
    {
      "name": "storage",
      "status": "Healthy",
      "description": "Free disk space: 82.3 GB",
      "duration": "00:00:00.0010000"
    },
    {
      "name": "dicom-listener",
      "status": "Healthy",
      "description": "DICOM SCP listening on port 11112",
      "duration": "00:00:00.0020000"
    }
  ]
}
```

**Response — Degraded (200 OK with degraded status):**
```json
{
  "status": "Degraded",
  "checks": [
    {
      "name": "database",
      "status": "Healthy"
    },
    {
      "name": "storage",
      "status": "Degraded",
      "description": "Free disk space low: 4.1 GB remaining"
    }
  ]
}
```

**Response — Unhealthy (503 Service Unavailable):**
```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "Cannot connect to PostgreSQL: Connection refused",
      "exception": "Npgsql.NpgsqlException: ..."
    }
  ]
}
```

### GET /diagnostics/health

Extended health check with component versions and uptime information. Requires authentication.

### GET /diagnostics/info

Returns runtime information: version, environment, uptime, host name. Requires authentication.

```json
{
  "version": "1.5.0+abc1234",
  "environment": "Production",
  "host": "hub-prod-01",
  "uptime": "2.14:32:11",
  "dotnetVersion": "10.0.0"
}
```

### Health check status codes

| HTTP Status | Health Status | Meaning |
|-------------|---------------|---------|
| 200 | Healthy | All checks passing |
| 200 | Degraded | Non-critical checks warning (disk space, connectivity) |
| 503 | Unhealthy | Critical checks failing (database unreachable) |

---

## Log Configuration

### Serilog file sink (daily rolling)

Logs are written to `./logs/` with daily rotation. Configured in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning",
        "Grpc": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "./logs/hub-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Seq sink (optional)

Enable centralized structured log search with Seq:

```json
{
  "Diagnostics": {
    "Seq": {
      "Enabled": true,
      "Url": "http://seq.internal.example.com:5341",
      "ApiKey": "your-seq-api-key"
    }
  }
}
```

Or via environment variables:

```env
Diagnostics__Seq__Enabled=true
Diagnostics__Seq__Url=http://seq.internal.example.com:5341
Diagnostics__Seq__ApiKey=your-seq-api-key
```

### HTTP sink (webhook / SIEM)

For forwarding logs to a SIEM or log aggregator via HTTP:

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Http",
        "Args": {
          "requestUri": "https://logs.internal.example.com/ingest",
          "batchPostingLimit": 100,
          "period": "00:00:05"
        }
      }
    ]
  }
}
```

---

## PHI Redaction

EdgeGuard Platform supports two PHI redaction modes for log outputs. **DICOM images and HL7 messages are never written to logs** in either mode — only structured metadata.

### Strict mode (recommended for production)

Set `Diagnostics__PHIRedaction=Strict`.

The following fields are redacted (replaced with `[REDACTED]`) in all log entries:

| Field | Example raw | Example redacted |
|-------|-------------|-----------------|
| Patient Name | `Smith^John` | `[REDACTED]` |
| Patient Date of Birth | `19800115` | `[REDACTED]` |
| Patient MRN / ID | `MRN123456` | `[REDACTED]` |
| Accession Number | `ACC20250101001` | `[REDACTED]` |
| Referring Physician | `Jones^Dr.` | `[REDACTED]` |

Study Instance UIDs and Series UIDs are preserved in Strict mode to allow correlation of log entries with DICOM data.

### Relaxed mode (development / staging only)

Set `Diagnostics__PHIRedaction=Relaxed`.

Patient name initials and a truncated MRN suffix may appear in log entries for easier debugging. **Never use Relaxed mode in a production environment containing real patient data.**

> **Security Note:** Log files should be treated as sensitive data in all environments. Restrict log file access to authorized system administrators only.

---

## Correlation ID Propagation

Every HTTP request receives a correlation ID used to trace the request through all log entries.

### Request ID header

Include `X-Request-Id` in requests to use a caller-supplied correlation ID:

```
GET /api/studies HTTP/1.1
X-Request-Id: my-trace-id-abc123
```

If not provided, the Hub generates a new UUID automatically.

### Log entry format

All log entries for a request include the correlation ID:

```
[2025-01-15 14:23:01.123 +00:00 INF] my-trace-id-abc123 StudiesController GET /api/studies → 200 OK (47ms)
[2025-01-15 14:23:01.124 +00:00 INF] my-trace-id-abc123 StudyRepository Queried 12 studies for node node-rad-a
```

### Seq query by correlation ID

```
CorrelationId = "my-trace-id-abc123"
```

### OpenTelemetry trace propagation

When OTel is enabled, the correlation ID is also propagated as a W3C trace context header (`traceparent`), enabling distributed tracing across the Hub and Node.

---

## OpenTelemetry and Prometheus

### Enabling OpenTelemetry

```json
{
  "Diagnostics": {
    "OpenTelemetry": {
      "Enabled": true,
      "OtlpEndpoint": "http://otel-collector.internal.example.com:4317",
      "ServiceName": "edgeguard-hub",
      "Metrics": true,
      "Traces": true
    }
  }
}
```

Or via environment variables:

```env
Diagnostics__OpenTelemetry__Enabled=true
Diagnostics__OpenTelemetry__OtlpEndpoint=http://otel-collector.internal.example.com:4317
```

### Prometheus metrics

When OpenTelemetry is enabled, Prometheus-compatible metrics are exposed at:

```
GET /metrics
```

Requires no authentication. Restrict access to the monitoring network via firewall or Nginx `allow/deny` directives.

Example Prometheus scrape configuration:

```yaml
scrape_configs:
  - job_name: edgeguard-hub
    static_configs:
      - targets: ['hub.internal.example.com:5000']
    metrics_path: /metrics
    scheme: https
    tls_config:
      insecure_skip_verify: false
```

---

## Key Metrics to Monitor

### Recommended alerts

| Metric | Warning threshold | Critical threshold | Notes |
|--------|------------------|--------------------|-------|
| Study receive rate (per minute) | Drop >50% vs 1h avg | Drop >90% | May indicate DICOM SCP issue |
| PACS send failure count | >5 per hour | >20 per hour | Check firewall and AE title |
| HL7 message queue depth | >100 | >500 | Check MLLP listener health |
| Node connectivity (last heartbeat) | >5 min | >15 min | Node may be offline |
| Disk free space | <20 GB | <5 GB | data/ directory growing |
| Memory usage | >80% | >95% | Possible queue buildup |
| HTTP 5xx rate | >1% | >5% | Application errors |
| DB connection pool exhaustion | Any | Any | Increase pool size |

### Custom Prometheus queries (PromQL examples)

```promql
# Study receive rate over 5 minutes
rate(edgeguard_dicom_studies_received_total[5m])

# PACS send failures in last hour
increase(edgeguard_pacs_send_failures_total[1h])

# HL7 messages queued
edgeguard_hl7_queue_depth

# Node offline (no heartbeat for 10 minutes)
time() - edgeguard_node_last_heartbeat_timestamp_seconds > 600
```

---

## Log Examples

### DICOM study received

```
[2025-01-15 09:12:34.001 INF] correlationId=dcm-recv-001
  DICOM C-STORE received: StudyInstanceUID=1.2.840.10008.5.1.4.1.1.2.1234,
  Modality=CT, Instances=256, CallingAE=CT_SCANNER_A, NodeId=node-rad-a
  Duration=1234ms
```

### HL7 message processed

```
[2025-01-15 09:13:00.123 INF] correlationId=hl7-0001
  HL7 ORM^O01 processed: MessageControlId=MSG001, ProcedureId=PROC123,
  StudyDate=20250115, Modality=CT — PatientId=[REDACTED] (Strict redaction)
  ProcessingDuration=12ms
```

### PACS send succeeded

```
[2025-01-15 09:15:42.789 INF] correlationId=pacs-send-001
  DICOM C-STORE SCU: Study sent to PACS [OrthanProd | AE=ORTHANC_PROD]
  StudyInstanceUID=1.2.840.10008.5.1.4.1.1.2.1234, Instances=256,
  Duration=8765ms, Status=Success
```

### PACS send failed

```
[2025-01-15 09:15:50.001 WRN] correlationId=pacs-send-002
  DICOM C-STORE SCU failed: PACS [OrthanProd | AE=ORTHANC_PROD]
  StudyInstanceUID=1.2.840.10008.5.1.4.1.1.2.5678
  Error=Association rejected (0x0111): Called AE Title not recognized
  RetryAttempt=1/3
```

### Node heartbeat received

```
[2025-01-15 09:16:00.001 DBG] correlationId=telemetry-hb
  Node heartbeat: NodeId=node-rad-a, Status=Online,
  DicomPort=11112, QueueDepth=0, MemoryMB=312, DiskFreeMB=51200
```
