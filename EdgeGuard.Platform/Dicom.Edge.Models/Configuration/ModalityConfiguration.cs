namespace Dicom.Edge.Models.Configuration
{
    /// <summary>
    /// Represents configuration for a DICOM modality (SCU) that can connect to this Edge Node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class defines authorized DICOM systems that can send images to the Edge Node.
    /// It includes connection parameters, security settings, routing preferences, and
    /// connection limits for comprehensive modality management.
    /// </para>
    /// <para>
    /// <strong>Security Model:</strong>
    /// The Edge Node can operate in two security modes:
    /// <list type="bullet">
    ///   <item><description><strong>Whitelist Mode:</strong> Only explicitly configured modalities can connect</description></item>
    ///   <item><description><strong>Open Mode:</strong> Any modality can connect (not recommended for production)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var ctConfig = new ModalityConfiguration
    /// {
    ///     AETitle = "CT_SCANNER_1",
    ///     IpAddress = "192.168.1.100",
    ///     Port = 104,
    ///     IsEnabled = true,
    ///     RequiresAuth = true,
    ///     DefaultDestination = "MAIN_PACS",
    ///     MaxConcurrentConnections = 5
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public class ModalityConfiguration
    {
        /// <summary>
        /// Gets or sets the unique identifier for this configuration.
        /// </summary>
        /// <remarks>
        /// Auto-generated GUID. Use as primary key in databases.
        /// </remarks>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Application Entity (AE) Title of the modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// DICOM identifier for the remote system.
        /// Maximum 16 characters (DICOM standard).
        /// </para>
        /// <para>
        /// <strong>Naming Conventions (recommended):</strong>
        /// <list type="bullet">
        ///   <item><description>CT_ROOM1, CT_ROOM2 - CT scanners by room</description></item>
        ///   <item><description>MR_UNIT_A - MRI systems</description></item>
        ///   <item><description>CR_PORTABLE_1 - Portable X-ray</description></item>
        ///   <item><description>US_DEPT_OB - Ultrasound by department</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string AETitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the display name for this modality (for UI purposes).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Human-readable name shown in dashboards and reports.
        /// </para>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"CT Scanner - Emergency Room 1"</description></item>
        ///   <item><description>"MRI Unit A - Radiology 3rd Floor"</description></item>
        ///   <item><description>"Portable X-Ray Unit 5"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used for IP-based access control validation.
        /// Can be IPv4 (e.g., "192.168.1.100") or IPv6 (e.g., "2001:db8::1").
        /// </para>
        /// <para>
        /// <strong>Security Note:</strong> Validate both AE Title AND IP address for stronger security.
        /// </para>
        /// </remarks>
        public string IpAddress { get; set; } = default!;

        /// <summary>
        /// Gets or sets the DICOM port number of the modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Standard DICOM port: 104
        /// Alternate ports: 11112, 2761, or custom ports
        /// </para>
        /// <para>
        /// Used for callback operations (C-MOVE, verification, etc.).
        /// </para>
        /// </remarks>
        public int Port { get; set; } = 104;

        /// <summary>
        /// Gets or sets whether this modality is enabled and allowed to connect.
        /// </summary>
        /// <remarks>
        /// <para>
        /// False = Connections from this modality will be rejected.
        /// Use to temporarily disable problematic modalities without deleting configuration.
        /// </para>
        /// </remarks>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether authentication/authorization is required for this modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// True = Validate AE Title and IP address match exactly
        /// False = Accept connections from this AE Title from any IP (less secure)
        /// </para>
        /// <para>
        /// <strong>Recommended:</strong> Always set to true in production environments.
        /// </para>
        /// </remarks>
        public bool RequiresAuth { get; set; } = true;

        /// <summary>
        /// Gets or sets the default destination AE Title for studies received from this modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Specifies where to route studies by default if no explicit routing rule matches.
        /// </para>
        /// <para>
        /// <strong>Use Cases:</strong>
        /// <list type="bullet">
        ///   <item><description>Emergency modalities → Emergency PACS</description></item>
        ///   <item><description>Cardiology modalities → Cardiology PACS</description></item>
        ///   <item><description>Research modalities → Research archive</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Null = Use global default destination.
        /// </para>
        /// </remarks>
        public string? DefaultDestination { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent DICOM associations allowed from this modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Prevents a single modality from monopolizing Edge Node resources.
        /// </para>
        /// <para>
        /// <strong>Recommended Values:</strong>
        /// <list type="bullet">
        ///   <item><description>CT/MR: 3-5 (multi-series studies)</description></item>
        ///   <item><description>CR/DR: 1-2 (typically single images)</description></item>
        ///   <item><description>US: 1-2 (smaller studies)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// 0 = Unlimited (not recommended)
        /// </para>
        /// </remarks>
        public int MaxConcurrentConnections { get; set; } = 5;

        /// <summary>
        /// Gets or sets the association timeout duration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// How long to wait for operations before closing the association.
        /// Prevents hung connections from consuming resources.
        /// </para>
        /// <para>
        /// <strong>Recommended:</strong> 5-10 minutes for most modalities.
        /// Increase for slow connections or large studies.
        /// </para>
        /// </remarks>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the manufacturer of the modality (for reference/support).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong> Siemens, GE, Philips, Canon, Fujifilm
        /// </para>
        /// <para>
        /// Useful for:
        /// <list type="bullet">
        ///   <item><description>Vendor-specific workarounds</description></item>
        ///   <item><description>Support ticket routing</description></item>
        ///   <item><description>Compatibility tracking</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the model name/number of the modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>CT: "SOMATOM Definition AS"</description></item>
        ///   <item><description>MRI: "MAGNETOM Skyra 3T"</description></item>
        ///   <item><description>CR: "FCR XG-1"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? ModelName { get; set; }

        /// <summary>
        /// Gets or sets the serial number of the modality.
        /// </summary>
        /// <remarks>
        /// Unique hardware identifier. Useful for warranty and service tracking.
        /// </remarks>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the physical location of the modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"Main Hospital - Radiology - Room 3"</description></item>
        ///   <item><description>"Outpatient Clinic - 2nd Floor"</description></item>
        ///   <item><description>"Mobile Unit #5"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets the department or clinical specialty.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong>
        /// Radiology, Cardiology, Orthopedics, Emergency, Oncology
        /// </para>
        /// <para>
        /// Used for:
        /// <list type="bullet">
        ///   <item><description>Department-specific routing</description></item>
        ///   <item><description>Billing allocation</description></item>
        ///   <item><description>Usage analytics</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Department { get; set; }

        /// <summary>
        /// Gets or sets additional notes or special configuration details.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Free text for:
        /// <list type="bullet">
        ///   <item><description>Known issues or workarounds</description></item>
        ///   <item><description>Maintenance schedules</description></item>
        ///   <item><description>Contact information</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this configuration was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp when this configuration was last modified.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the last successful connection from this modality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Null if modality has never connected.
        /// Used for:
        /// <list type="bullet">
        ///   <item><description>Detecting inactive/decommissioned modalities</description></item>
        ///   <item><description>Monitoring connection health</description></item>
        ///   <item><description>Audit trails</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public DateTime? LastConnectionAt { get; set; }

        /// <summary>
        /// Gets or sets whether this modality is currently active/online.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Updated by verification (C-ECHO) checks.
        /// True = Last verification succeeded
        /// False = Last verification failed or never verified
        /// </para>
        /// </remarks>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Gets the display information for logging and UI.
        /// </summary>
        public string DisplayInfo => DisplayName ?? $"{AETitle} ({IpAddress}:{Port})";
    }
}
