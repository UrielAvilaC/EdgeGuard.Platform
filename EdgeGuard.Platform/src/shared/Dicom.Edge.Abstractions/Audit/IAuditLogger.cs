namespace Dicom.Edge.Abstractions.Audit
{
    /// <summary>
    /// Service for logging audit events for compliance and security monitoring.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Audit logging is critical for:
    /// <list type="bullet">
    ///   <item><description>HIPAA compliance (45 CFR § 164.312(b))</description></item>
    ///   <item><description>GDPR Article 30 (Records of processing activities)</description></item>
    ///   <item><description>Security incident investigation</description></item>
    ///   <item><description>Regulatory audits</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IAuditLogger
    {
        /// <summary>
        /// Logs an audit event asynchronously.
        /// </summary>
        /// <param name="eventType">Type of event.</param>
        /// <param name="action">Action performed.</param>
        /// <param name="details">Event details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogEventAsync(
            string eventType,
            string action,
            string? details = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs study-related access or operations.
        /// </summary>
        /// <param name="studyInstanceUid">Study being accessed.</param>
        /// <param name="userId">User performing the action.</param>
        /// <param name="action">Action performed (viewed, sent, deleted, etc.).</param>
        /// <param name="isSuccess">Whether the action succeeded.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogStudyAccessAsync(
            string studyInstanceUid,
            string? userId,
            string action,
            bool isSuccess = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs security-related events (authentication, authorization failures, etc.).
        /// </summary>
        /// <param name="eventType">Security event type.</param>
        /// <param name="userId">User involved.</param>
        /// <param name="details">Event details.</param>
        /// <param name="severity">Severity level (0=Info, 1=Warning, 2=Error, 3=Critical).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogSecurityEventAsync(
            string eventType,
            string? userId,
            string details,
            int severity = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs DICOM association events.
        /// </summary>
        /// <param name="callingAeTitle">Source AE Title.</param>
        /// <param name="calledAeTitle">Destination AE Title.</param>
        /// <param name="action">Association action (requested, accepted, rejected, aborted).</param>
        /// <param name="details">Additional details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogAssociationEventAsync(
            string callingAeTitle,
            string calledAeTitle,
            string action,
            string? details = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs system configuration changes.
        /// </summary>
        /// <param name="userId">User who made the change.</param>
        /// <param name="configType">Type of configuration changed.</param>
        /// <param name="changes">Description of changes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogConfigurationChangeAsync(
            string userId,
            string configType,
            string changes,
            CancellationToken cancellationToken = default);
    }
}
