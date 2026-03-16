using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Edge
{
    /// <summary>
    /// Represents a detailed record of an error that occurred during study transfer or processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transfer errors are critical for diagnosing failures in production environments.
    /// This class captures comprehensive error information including context, stack traces,
    /// and retry attempts for troubleshooting and analytics.
    /// </para>
    /// <para>
    /// <strong>Key Use Cases:</strong>
    /// <list type="bullet">
    ///   <item><description><strong>Troubleshooting:</strong> Diagnose recurring transfer failures</description></item>
    ///   <item><description><strong>Monitoring:</strong> Alert on error patterns or spikes</description></item>
    ///   <item><description><strong>Analytics:</strong> Identify most common failure types</description></item>
    ///   <item><description><strong>Retry Logic:</strong> Track retry attempts and outcomes</description></item>
    ///   <item><description><strong>Reporting:</strong> Generate reliability and uptime reports</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// try
    /// {
    ///     await SendStudyToHub(study);
    /// }
    /// catch (HttpRequestException ex)
    /// {
    ///     var error = new TransferError
    ///     {
    ///         StudyInstanceUid = study.StudyInstanceUid,
    ///         ErrorType = ErrorType.Network,
    ///         ErrorMessage = "Failed to connect to Hub",
    ///         StackTrace = ex.StackTrace,
    ///         RetryAttempt = currentRetry,
    ///         AdditionalContext = new Dictionary&lt;string, string&gt;
    ///         {
    ///             ["HubEndpoint"] = hubUrl,
    ///             ["InstanceCount"] = study.InstanceCount.ToString()
    ///         }
    ///     };
    ///     await _errorRepository.SaveAsync(error);
    ///     
    ///     // Schedule retry if applicable
    ///     if (error.IsRetryable)
    ///     {
    ///         await _retryQueue.EnqueueAsync(study, delay: TimeSpan.FromMinutes(5));
    ///     }
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Database Indexing:</strong>
    /// For optimal query performance, index on:
    /// <list type="bullet">
    ///   <item><description>StudyInstanceUid (queries by study)</description></item>
    ///   <item><description>OccurredAt (time-range queries)</description></item>
    ///   <item><description>ErrorType (error type analytics)</description></item>
    ///   <item><description>IsResolved (active error tracking)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class TransferError
    {
        /// <summary>
        /// Gets or sets the unique identifier for this error record.
        /// </summary>
        /// <remarks>
        /// Auto-generated GUID. Use as primary key in databases.
        /// </remarks>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Study Instance UID of the study that encountered the error.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Links this error to the specific DICOM study for traceability.
        /// Null if error occurred outside study context (e.g., system-level error).
        /// </para>
        /// <para>
        /// Use to:
        /// <list type="bullet">
        ///   <item><description>View all errors for a specific study</description></item>
        ///   <item><description>Correlate errors with study characteristics</description></item>
        ///   <item><description>Track retry attempts per study</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? StudyInstanceUid { get; set; }

        /// <summary>
        /// Gets or sets the category of error that occurred.
        /// </summary>
        /// <remarks>
        /// See <see cref="ErrorType"/> for detailed error type descriptions.
        /// Used for filtering, alerting, and retry decision-making.
        /// </remarks>
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// Gets or sets a human-readable description of the error.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Should be clear and actionable. Include:
        /// <list type="bullet">
        ///   <item><description>What operation was being performed</description></item>
        ///   <item><description>What went wrong</description></item>
        ///   <item><description>Relevant context (endpoint, file path, etc.)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Good Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"Failed to connect to Hub at https://hub.example.com: Connection timed out after 30s"</description></item>
        ///   <item><description>"Cannot save DICOM instance to /data/studies: Disk full (0 bytes available)"</description></item>
        ///   <item><description>"Calling AE Title 'UNKNOWN_CT' not found in authorized list"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string ErrorMessage { get; set; } = default!;

        /// <summary>
        /// Gets or sets the full stack trace of the exception.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Optional but highly recommended for debugging.
        /// Provides exact code location and call chain leading to the error.
        /// </para>
        /// <para>
        /// Can be large - consider:
        /// <list type="bullet">
        ///   <item><description>Truncating very long stack traces (e.g., &gt; 10KB)</description></item>
        ///   <item><description>Storing in separate table/blob for large deployments</description></item>
        ///   <item><description>Hashing identical stack traces to reduce storage</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the error occurred.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use UTC for timezone-independent storage and analysis.
        /// Critical for:
        /// <list type="bullet">
        ///   <item><description>Time-series analysis of errors</description></item>
        ///   <item><description>Correlating errors with system events</description></item>
        ///   <item><description>SLA and uptime calculations</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the retry attempt number when this error occurred.
        /// </summary>
        /// <remarks>
        /// <para>
        /// 0 = first attempt (not a retry)
        /// 1 = first retry
        /// 2 = second retry, etc.
        /// </para>
        /// <para>
        /// Use to:
        /// <list type="bullet">
        ///   <item><description>Track how many retries were attempted</description></item>
        ///   <item><description>Determine if error is persistent or transient</description></item>
        ///   <item><description>Trigger escalation after N retries</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int RetryAttempt { get; set; }

        /// <summary>
        /// Gets or sets whether this error has been resolved (e.g., by successful retry or manual intervention).
        /// </summary>
        /// <remarks>
        /// <para>
        /// False = error is still active/unresolved
        /// True = error has been resolved
        /// </para>
        /// <para>
        /// Update to true when:
        /// <list type="bullet">
        ///   <item><description>Subsequent retry succeeds</description></item>
        ///   <item><description>Administrator marks as resolved</description></item>
        ///   <item><description>Issue is fixed and operation completes</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the error was resolved.
        /// </summary>
        /// <remarks>
        /// Null if error is not yet resolved.
        /// Calculate time-to-resolution: <c>ResolvedAt - OccurredAt</c>
        /// </remarks>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Gets or sets notes about how the error was resolved.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>"Resolved automatically on retry #3"</description></item>
        ///   <item><description>"Admin increased disk space and requeued study"</description></item>
        ///   <item><description>"Fixed by updating routing configuration"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Edge Node where the error occurred.
        /// </summary>
        /// <remarks>
        /// Useful in multi-node deployments to identify problematic nodes.
        /// </remarks>
        public string? EdgeNodeId { get; set; }

        /// <summary>
        /// Gets or sets additional contextual information about the error as key-value pairs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Store as JSON string for database compatibility.
        /// Useful context to include:
        /// <list type="bullet">
        ///   <item><description>Destination endpoint/AE Title</description></item>
        ///   <item><description>File paths involved</description></item>
        ///   <item><description>Configuration values in use</description></item>
        ///   <item><description>Network conditions (latency, bandwidth)</description></item>
        ///   <item><description>Related operation IDs</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example JSON:</strong>
        /// <code>
        /// {
        ///   "HubEndpoint": "https://hub.example.com",
        ///   "InstanceCount": "150",
        ///   "TotalSizeMB": "500",
        ///   "NetworkLatencyMs": "250"
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        public string? AdditionalContext { get; set; }

        /// <summary>
        /// Gets whether this error type is typically retryable.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Based on ErrorType:
        /// <list type="bullet">
        ///   <item><description>Network: Usually retryable</description></item>
        ///   <item><description>Storage: May be retryable after cleanup</description></item>
        ///   <item><description>DICOM: Not retryable (data issue)</description></item>
        ///   <item><description>Configuration: Not retryable (requires config change)</description></item>
        ///   <item><description>Authentication: Not retryable (requires credential update)</description></item>
        ///   <item><description>Processing: May be retryable</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool IsRetryable => ErrorType switch
        {
            ErrorType.Network => true,
            ErrorType.Storage => false, // Usually needs intervention
            ErrorType.Dicom => false,
            ErrorType.Configuration => false,
            ErrorType.Authentication => false,
            ErrorType.Processing => true,
            _ => false
        };
    }
}
