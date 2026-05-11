namespace Dicom.Edge.Node;

/// <summary>
/// Centralized constants for the Edge Node host application.
/// Eliminates hardcoded strings from <c>Program.cs</c>, <c>DicomInstanceHandler</c>,
/// and other host-level components.
/// </summary>
internal static class NodeConstants
{
    // ── Bootstrap ─────────────────────────────────────────────────────────────

    /// <summary>Bootstrap log file path used before the DI container is ready.</summary>
    public const string BootstrapLogPath = "logs/node-bootstrap-.log";

    /// <summary>Optional diagnostics appsettings file name.</summary>
    public const string DiagnosticsSettingsFile = "appsettings.diagnostics.json";

    // ── Connection Strings ────────────────────────────────────────────────────

    /// <summary>Name of the connection string in <c>ConnectionStrings</c> section.</summary>
    public const string ConnectionStringName = "NodeDatabase";

    /// <summary>Fallback SQLite connection string when nothing is configured.</summary>
    public const string DefaultConnectionString = "Data Source=edge-node.db";

    // ── Configuration Keys ────────────────────────────────────────────────────

    /// <summary>Configuration key for the Node API HTTP port.</summary>
    public const string NodeApiPortKey = "NodeApi:Port";

    /// <summary>Default port for the Node API.</summary>
    public const int DefaultNodeApiPort = 5120;

    // ── DICOM File Storage ────────────────────────────────────────────────────

    /// <summary>File extension for DICOM files.</summary>
    public const string DicomFileExtension = ".dcm";

    /// <summary>Temporary file extension used during atomic writes.</summary>
    public const string TempFileExtension = ".tmp";

    // ── DICOM Defaults ────────────────────────────────────────────────────────

    /// <summary>Default Patient ID when none is provided in the DICOM dataset.</summary>
    public const string DefaultPatientId = "UNKNOWN";

    /// <summary>Default modality when none is provided in the DICOM dataset.</summary>
    public const string DefaultModality = "OT";

    // ── Shadow Property Names (EF Core) ───────────────────────────────────────

    /// <summary>Shadow property name for instance file size in bytes.</summary>
    public const string ShadowPropertyFileSizeBytes = "file_size_bytes";

    /// <summary>Shadow property name for DICOM transfer syntax UID.</summary>
    public const string ShadowPropertyTransferSyntaxUid = "transfer_syntax_uid";

    // ── Health ────────────────────────────────────────────────────────────────

    /// <summary>Status string returned by the health endpoint when the node is healthy.</summary>
    public const string HealthStatusHealthy = "Healthy";

    // ── HTTP Headers ──────────────────────────────────────────────────────────

    /// <summary>API key header name for Hub-to-Node authentication.</summary>
    public const string ApiKeyHeaderName = "X-Api-Key";
}
