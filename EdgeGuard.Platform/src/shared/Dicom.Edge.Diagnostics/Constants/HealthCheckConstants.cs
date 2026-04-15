namespace Dicom.Edge.Diagnostics.Constants;

/// <summary>
/// Standardized constants for health check registrations, endpoint paths,
/// tag names, and related diagnostics infrastructure shared across all platform components.
/// </summary>
public static class HealthCheckConstants
{
    // ==================== Endpoint Paths ====================

    /// <summary>Liveness probe endpoint path. Returns healthy if the application is running.</summary>
    public const string LivenessEndpoint = "/health/live";

    /// <summary>Readiness probe endpoint path. Checks dependent services (storage, DB, etc.).</summary>
    public const string ReadinessEndpoint = "/health/ready";

    // ==================== Tags ====================

    /// <summary>Tag indicating the health check participates in readiness evaluation.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Tag for storage-related health checks.</summary>
    public const string StorageTag = "storage";

    /// <summary>Tag for PACS-related health checks.</summary>
    public const string PacsTag = "pacs";

    /// <summary>Tag for HL7-related health checks.</summary>
    public const string Hl7Tag = "hl7";

    /// <summary>Tag for database-related health checks.</summary>
    public const string DatabaseTag = "database";

    // ==================== Check Names ====================

    /// <summary>Registered name for the storage health check.</summary>
    public const string StorageCheckName = "storage";

    /// <summary>Registered name for the PACS connectivity health check.</summary>
    public const string PacsCheckName = "pacs";

    /// <summary>Registered name for the HL7 listener health check.</summary>
    public const string Hl7ListenerCheckName = "hl7-listener";

    /// <summary>Registered name for the database health check.</summary>
    public const string DatabaseCheckName = "database";

    // ==================== Descriptors ====================

    /// <summary>Standard JSON content type for health check responses.</summary>
    public const string JsonContentType = "application/json";

    /// <summary>Reason reported when a health check times out.</summary>
    public const string TimeoutReason = "Timeout";
}
