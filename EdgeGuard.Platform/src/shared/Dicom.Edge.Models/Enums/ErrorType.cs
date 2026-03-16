namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Categorizes the type of error that occurred during study processing or transfer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Error types help diagnose and troubleshoot failures by categorizing issues into
    /// distinct areas: network, storage, DICOM protocol, or configuration.
    /// </para>
    /// <para>
    /// <strong>Use for:</strong>
    /// <list type="bullet">
    ///   <item><description>Error dashboards and alerting</description></item>
    ///   <item><description>Root cause analysis</description></item>
    ///   <item><description>Automated retry logic (e.g., retry network errors, not DICOM errors)</description></item>
    ///   <item><description>Performance and reliability metrics</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum ErrorType
    {
        /// <summary>
        /// Network-related errors (connection failures, timeouts, DNS issues).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Cannot connect to destination PACS/Hub</description></item>
        ///   <item><description>Connection dropped mid-transfer</description></item>
        ///   <item><description>Timeout waiting for response</description></item>
        ///   <item><description>DNS resolution failed</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> Usually transient - safe to retry with exponential backoff.
        /// </para>
        /// </remarks>
        Network = 0,

        /// <summary>
        /// Storage-related errors (disk full, permission issues, I/O failures).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Disk full - cannot save DICOM instance</description></item>
        ///   <item><description>Permission denied writing to storage path</description></item>
        ///   <item><description>I/O error reading/writing file</description></item>
        ///   <item><description>Corrupted file system</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> May be persistent - requires intervention (e.g., free disk space).
        /// </para>
        /// </remarks>
        Storage = 1,

        /// <summary>
        /// DICOM protocol errors (invalid data, unsupported SOP class, protocol violations).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Malformed DICOM file (invalid header, missing required tags)</description></item>
        ///   <item><description>Unsupported SOP Class or Transfer Syntax</description></item>
        ///   <item><description>Protocol violation during association</description></item>
        ///   <item><description>Invalid DIMSE command sequence</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> Not recoverable - data or configuration issue.
        /// </para>
        /// </remarks>
        Dicom = 2,

        /// <summary>
        /// Configuration errors (missing settings, invalid routing rules, unauthorized access).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Calling AE Title not authorized</description></item>
        ///   <item><description>No routing rule matches study criteria</description></item>
        ///   <item><description>Missing or invalid configuration setting</description></item>
        ///   <item><description>Destination endpoint not configured</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> Not recoverable - requires configuration update.
        /// </para>
        /// </remarks>
        Configuration = 3,

        /// <summary>
        /// Authentication or authorization errors.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Invalid API key or credentials</description></item>
        ///   <item><description>Token expired</description></item>
        ///   <item><description>Insufficient permissions</description></item>
        ///   <item><description>Certificate validation failed</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> Not recoverable without credential update.
        /// </para>
        /// </remarks>
        Authentication = 4,

        /// <summary>
        /// Processing errors during study manipulation (anonymization, compression, etc.).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Common Scenarios:</strong>
        /// <list type="bullet">
        ///   <item><description>Failed to anonymize DICOM tags</description></item>
        ///   <item><description>Image compression failed</description></item>
        ///   <item><description>Metadata extraction error</description></item>
        ///   <item><description>Business rule validation failed</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Retry Strategy:</strong> May be transient or persistent depending on cause.
        /// </para>
        /// </remarks>
        Processing = 5,

        /// <summary>
        /// Unknown or uncategorized errors.
        /// </summary>
        /// <remarks>
        /// Use when error cannot be classified into other categories.
        /// Review and recategorize when possible for better analytics.
        /// </remarks>
        Unknown = 99
    }
}
