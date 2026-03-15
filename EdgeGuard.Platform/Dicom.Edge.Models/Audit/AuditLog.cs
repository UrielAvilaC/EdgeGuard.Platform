using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Audit
{
    /// <summary>
    /// Represents an audit trail entry for compliance and security monitoring.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Audit logs provide a tamper-evident record of system activities for:
    /// <list type="bullet">
    ///   <item><description>HIPAA compliance (45 CFR § 164.312(b) - Audit Controls)</description></item>
    ///   <item><description>GDPR Article 30 (Records of processing activities)</description></item>
    ///   <item><description>Security incident investigation</description></item>
    ///   <item><description>Regulatory audits and reporting</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class AuditLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of event being audited.
        /// </summary>
        public AuditEventType EventType { get; set; }
        
        /// <summary>
        /// Study Instance UID if event relates to a specific study.
        /// </summary>
        public string? StudyInstanceUid { get; set; }
        
        /// <summary>
        /// Source system AE Title.
        /// </summary>
        public string? SourceAeTitle { get; set; }
        
        /// <summary>
        /// Destination system AE Title.
        /// </summary>
        public string? DestinationAeTitle { get; set; }
        
        /// <summary>
        /// Action performed (sent, received, viewed, deleted, etc.).
        /// </summary>
        public string Action { get; set; } = default!;
        
        /// <summary>
        /// UTC timestamp when event occurred.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User ID who performed the action (if applicable).
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// User display name.
        /// </summary>
        public string? UserName { get; set; }
        
        /// <summary>
        /// IP address of user/system performing action.
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// Edge Node ID where event occurred.
        /// </summary>
        public string? EdgeNodeId { get; set; }
        
        /// <summary>
        /// Patient ID (for patient-level audit trails).
        /// </summary>
        /// <remarks>
        /// Store hashed/tokenized version for privacy.
        /// </remarks>
        public string? PatientId { get; set; }
        
        /// <summary>
        /// Success status of the action.
        /// </summary>
        public bool IsSuccess { get; set; } = true;
        
        /// <summary>
        /// Error message if action failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Additional contextual details as JSON.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Store relevant context like:
        /// <list type="bullet">
        ///   <item><description>Number of instances</description></item>
        ///   <item><description>Data size transferred</description></item>
        ///   <item><description>Configuration changes made</description></item>
        ///   <item><description>Before/after values</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? Details { get; set; }
        
        /// <summary>
        /// Severity level for alerting/filtering.
        /// </summary>
        /// <remarks>
        /// Information = 0, Warning = 1, Error = 2, Critical = 3
        /// </remarks>
        public int Severity { get; set; }
    }
}
