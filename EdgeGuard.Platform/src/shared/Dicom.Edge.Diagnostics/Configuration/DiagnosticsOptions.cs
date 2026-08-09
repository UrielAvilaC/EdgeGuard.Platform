namespace Dicom.Edge.Diagnostics.Configuration;

/// <summary>
/// Root configuration options for platform diagnostics, logging, health checks,
/// metrics and PHI redaction. Shared by Hub and Edge Node.
/// </summary>
public sealed class DiagnosticsOptions
{
    /// <summary>Configuration section name in appsettings.</summary>
    public const string SectionName = "Diagnostics";

    /// <summary>
    /// Unique identifier for this instance (Hub or Edge Node).
    /// Defaults to machine name if not set.
    /// </summary>
    public string InstanceId { get; set; } = global::System.Environment.MachineName;

    /// <summary>Application name used in log enrichment and telemetry.</summary>
    public string Application { get; set; } = "EdgeGuard";

    /// <summary>Logical component name (e.g., DicomServer, Hl7Pipeline, Sender, Queue).</summary>
    public string Component { get; set; } = "Core";

    /// <summary>Environment name (Development, Staging, Production).</summary>
    public string Environment { get; set; } = "Production";

    /// <summary>OpenTelemetry meter name for this instance.</summary>
    public string MeterName { get; set; } = "Dicom.Edge";

    /// <summary>OpenTelemetry activity source name for distributed tracing.</summary>
    public string ActivitySourceName { get; set; } = "Dicom.Edge";

    /// <summary>File logging configuration.</summary>
    public FileLoggingOptions File { get; set; } = new();

    /// <summary>Seq sink configuration.</summary>
    public SeqOptions Seq { get; set; } = new();

    /// <summary>HTTP sink configuration for generic log shipping.</summary>
    public HttpSinkOptions HttpSink { get; set; } = new();

    /// <summary>OpenTelemetry configuration.</summary>
    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();

    /// <summary>PHI redaction configuration.</summary>
    public PhiRedactionOptions Redaction { get; set; } = new();

    /// <summary>Health check thresholds.</summary>
    public HealthCheckThresholdOptions HealthChecks { get; set; } = new();

    /// <summary>Correlation ID header name used for request tracing.</summary>
    public string CorrelationIdHeader { get; set; } = "X-Request-Id";
}

/// <summary>File-based logging sink configuration.</summary>
public sealed class FileLoggingOptions
{
    /// <summary>Base directory for log files.</summary>
    public string Path { get; set; } = "logs";

    /// <summary>Log file name template. Supports Serilog date tokens.</summary>
    public string FileNameTemplate { get; set; } = "platform-.log";

    /// <summary>Maximum size of a single log file in megabytes before rolling.</summary>
    public int MaxFileSizeMb { get; set; } = 100;

    /// <summary>Number of days to retain log files.</summary>
    public int RetainDays { get; set; } = 30;

    /// <summary>Maximum number of retained log files. Null for unlimited.</summary>
    public int? RetainedFileCountLimit { get; set; } = 31;

    /// <summary>Rolling interval for log files.</summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>Whether to use compact JSON formatting.</summary>
    public bool UseCompactJson { get; set; } = true;

    /// <summary>Per-DICOM-association log file configuration.</summary>
    public PerAssociationLoggingOptions PerAssociation { get; set; } = new();
}

/// <summary>
/// Configuration for the per-DICOM-association log files. One file is written per association,
/// from the A-ASSOCIATE-RQ until release/abort/close, in addition to (never instead of) the
/// global log.
/// </summary>
public sealed class PerAssociationLoggingOptions
{
    /// <summary>Master switch. When false the node behaves exactly as before.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Base directory for association logs. One sub-directory per day is created.</summary>
    public string Path { get; set; } = "logs/associations";

    /// <summary>Minimum level written to the association file (independent of the global log).</summary>
    public string MinimumLevel { get; set; } = "Debug";

    /// <summary>Write compact JSON instead of human-readable text.</summary>
    public bool UseCompactJson { get; set; }

    /// <summary>Days to keep association logs before the cleanup service removes them.</summary>
    public int RetainDays { get; set; } = 14;

    /// <summary>Safety cap on files created per day (protects against association floods).</summary>
    public int MaxFilesPerDay { get; set; } = 5000;

    /// <summary>Maximum size of a single association file in megabytes.</summary>
    public int MaxFileSizeMb { get; set; } = 10;

    /// <summary>Maximum total size of the association log directory in megabytes.</summary>
    public int MaxTotalSizeMb { get; set; } = 2048;

    /// <summary>Maximum number of association files kept open simultaneously.</summary>
    public int MaxOpen { get; set; } = 20;

    /// <summary>Include fo-dicom's own PDU/DIMSE events in the association file.</summary>
    public bool IncludeFoDicomInternals { get; set; } = true;
}

/// <summary>Seq centralized logging sink configuration.</summary>
public sealed class SeqOptions
{
    /// <summary>Whether the Seq sink is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Seq server URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Seq API key for authentication.</summary>
    public string? ApiKey { get; set; }
}

/// <summary>HTTP sink configuration for shipping logs to a remote endpoint.</summary>
public sealed class HttpSinkOptions
{
    /// <summary>Whether the HTTP sink is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Target HTTP endpoint URL.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Batch size for HTTP log shipping.</summary>
    public int BatchPostingLimit { get; set; } = 1000;

    /// <summary>Period in seconds between batch posts.</summary>
    public int PeriodSeconds { get; set; } = 5;
}

/// <summary>OpenTelemetry tracing and metrics configuration.</summary>
public sealed class OpenTelemetryOptions
{
    /// <summary>Whether OpenTelemetry is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>OTLP exporter endpoint.</summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>Service name reported to the collector.</summary>
    public string ServiceName { get; set; } = "EdgeGuard";

    /// <summary>Service version reported to the collector.</summary>
    public string? ServiceVersion { get; set; }

    /// <summary>Prometheus metrics configuration.</summary>
    public PrometheusOptions Prometheus { get; set; } = new();
}

/// <summary>Prometheus metrics exporter configuration.</summary>
public sealed class PrometheusOptions
{
    /// <summary>Whether the Prometheus exporter is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Endpoint path for Prometheus scraping.</summary>
    public string Endpoint { get; set; } = "/metrics";
}

/// <summary>PHI (Protected Health Information) redaction configuration.</summary>
public sealed class PhiRedactionOptions
{
    /// <summary>Whether PHI redaction is enabled. Enabled by default in Production.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Redaction mode: Strict (production) or Relaxed (development).</summary>
    public RedactionMode Mode { get; set; } = RedactionMode.Strict;

    /// <summary>Replacement string for redacted values.</summary>
    public string ReplacementToken { get; set; } = "[REDACTED]";

    /// <summary>Property names that must always be redacted.</summary>
    public List<string> RedactedProperties { get; set; } =
    [
        "PatientName",
        "PatientID",
        "PatientBirthDate",
        "PatientSex",
        "PatientAddress",
        "PatientTelephoneNumbers",
        "OtherPatientIDs",
        "OtherPatientNames",
        "ReferringPhysicianName",
        "InstitutionName",
        "InstitutionAddress"
    ];

    /// <summary>Regex patterns to detect and redact PHI in log messages.</summary>
    public List<string> RedactionPatterns { get; set; } =
    [
        @"\b\d{3}-\d{2}-\d{4}\b",       // SSN
        @"\b\d{10,15}\b",                // MRN / long numeric IDs
    ];
}

/// <summary>Redaction strictness mode.</summary>
public enum RedactionMode
{
    /// <summary>Strict: all PHI properties and patterns are redacted. Recommended for production.</summary>
    Strict,

    /// <summary>Relaxed: only explicitly listed properties are redacted. Suitable for development.</summary>
    Relaxed
}

/// <summary>Health check threshold configuration.</summary>
public sealed class HealthCheckThresholdOptions
{
    /// <summary>Path to the storage directory to monitor.</summary>
    public string? StoragePath { get; set; }

    /// <summary>Minimum available storage in MB before reporting unhealthy.</summary>
    public long StorageMinAvailableMb { get; set; } = 500;

    /// <summary>Maximum queue depth before reporting degraded.</summary>
    public int QueueMaxPendingItems { get; set; } = 1000;

    /// <summary>Timeout in seconds for external connectivity checks.</summary>
    public int ConnectivityTimeoutSeconds { get; set; } = 10;
}
