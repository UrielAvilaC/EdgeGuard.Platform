using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dicom.Edge.Contracts.Audit
{
    /// <summary>
    /// Data transfer object for audit log entries with HIPAA/GDPR compliance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This DTO represents audit log entries for:
    /// <list type="bullet">
    ///   <item><description><strong>HIPAA Compliance:</strong> 45 CFR § 164.312(b) - Audit Controls</description></item>
    ///   <item><description><strong>GDPR Compliance:</strong> Article 30 - Records of processing activities</description></item>
    ///   <item><description><strong>Security Monitoring:</strong> Unauthorized access detection</description></item>
    ///   <item><description><strong>Forensics:</strong> Incident investigation</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Retention Requirements:</strong>
    /// <list type="bullet">
    ///   <item><description>HIPAA: 6 years minimum</description></item>
    ///   <item><description>GDPR: Varies by jurisdiction (typically 1-7 years)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class AuditLogDto
    {
        /// <summary>
        /// Unique identifier for this audit log entry.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Id { get; set; } = default!;

        /// <summary>
        /// Type of event being audited.
        /// </summary>
        [Required]
        [Display(Name = "Event Type")]
        [StringLength(100)]
        public string EventType { get; set; } = default!;

        /// <summary>
        /// Numeric event type code for efficient filtering.
        /// </summary>
        /// <remarks>
        /// 0-9: Study lifecycle, 10-19: Association, 20-29: System, 30-39: Security, 40-49: Data access
        /// </remarks>
        [Range(0, 99)]
        public int EventTypeCode { get; set; }

        /// <summary>
        /// Study Instance UID if this event relates to a specific study.
        /// </summary>
        [StringLength(64)]
        [Display(Name = "Study UID")]
        public string? StudyInstanceUid { get; set; }

        /// <summary>
        /// Source DICOM Application Entity Title.
        /// </summary>
        [StringLength(16)]
        [Display(Name = "Source AE Title")]
        public string? SourceAeTitle { get; set; }

        /// <summary>
        /// Destination DICOM Application Entity Title.
        /// </summary>
        [StringLength(16)]
        [Display(Name = "Destination AE Title")]
        public string? DestinationAeTitle { get; set; }

        /// <summary>
        /// Action performed (e.g., "Received", "Sent", "Deleted").
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = default!;

        /// <summary>
        /// UTC timestamp when the event occurred.
        /// </summary>
        [Required]
        [Display(Name = "Timestamp (UTC)")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Unique identifier of the user who performed the action.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "User ID")]
        public string? UserId { get; set; }

        /// <summary>
        /// Display name of the user.
        /// </summary>
        [StringLength(200)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        /// <summary>
        /// IP address from which the action originated.
        /// </summary>
        [StringLength(45)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// ID of the Edge Node where the event occurred.
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Edge Node ID")]
        public string? EdgeNodeId { get; set; }

        /// <summary>
        /// Patient ID (should be tokenized/hashed for PHI protection).
        /// </summary>
        /// <remarks>
        /// ⚠️ PHI WARNING: This field contains Protected Health Information.
        /// Should be encrypted in transit and at rest.
        /// </remarks>
        [StringLength(100)]
        [Display(Name = "Patient ID")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PatientId { get; set; }

        /// <summary>
        /// Whether the action completed successfully.
        /// </summary>
        [Required]
        [Display(Name = "Success")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if the action failed.
        /// </summary>
        [StringLength(1000)]
        [Display(Name = "Error Message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional contextual details as JSON string.
        /// </summary>
        [StringLength(4000)]
        public string? Details { get; set; }

        /// <summary>
        /// Severity level: 0=Info, 1=Warning, 2=Error, 3=Critical.
        /// </summary>
        [Required]
        [Range(0, 3)]
        [Display(Name = "Severity")]
        public int Severity { get; set; }

        // ==================== Additional Compliance Fields ====================

        /// <summary>
        /// Correlation ID for tracking related events.
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Correlation ID")]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Session ID of the user session.
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Session ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Application or module that generated this audit entry.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Application")]
        public string? Application { get; set; }

        /// <summary>
        /// Version of the application.
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Version")]
        public string? ApplicationVersion { get; set; }

        /// <summary>
        /// Whether this event is subject to legal hold.
        /// </summary>
        [Display(Name = "Legal Hold")]
        public bool IsLegalHold { get; set; }

        /// <summary>
        /// Retention period in days for this audit log.
        /// </summary>
        [Range(1, 3650)]
        [Display(Name = "Retention Days")]
        public int? RetentionDays { get; set; }

        /// <summary>
        /// Category of the event for classification.
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Severity description for display.
        /// </summary>
        [JsonIgnore]
        public string SeverityDescription => Severity switch
        {
            0 => "Information",
            1 => "Warning",
            2 => "Error",
            3 => "Critical",
            _ => "Unknown"
        };

        /// <summary>
        /// Whether this is a security-related event.
        /// </summary>
        [JsonIgnore]
        public bool IsSecurityEvent => EventTypeCode >= 30 && EventTypeCode < 40;

        /// <summary>
        /// Whether this is a data access event (PHI accessed).
        /// </summary>
        [JsonIgnore]
        public bool IsDataAccessEvent => EventTypeCode >= 40 && EventTypeCode < 50;

        /// <summary>
        /// Whether this is a high-priority event requiring immediate attention.
        /// </summary>
        [JsonIgnore]
        public bool IsHighPriority => Severity >= 2 || !IsSuccess || IsSecurityEvent;

        /// <summary>
        /// Formatted display string for logging.
        /// </summary>
        [JsonIgnore]
        public string DisplayText => 
            $"[{SeverityDescription}] {EventType}: {Action} by {UserName ?? "System"} " +
            $"at {Timestamp:yyyy-MM-dd HH:mm:ss} UTC " +
            $"{(IsSuccess ? "✓" : $"✗ ({ErrorMessage})")}";
    }
}
