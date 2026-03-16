namespace Dicom.Edge.Abstractions.Metrics
{
    /// <summary>
    /// Service for collecting and recording application metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metrics enable:
    /// <list type="bullet">
    ///   <item><description>Performance monitoring and alerting</description></item>
    ///   <item><description>Capacity planning</description></item>
    ///   <item><description>Troubleshooting and diagnostics</description></item>
    ///   <item><description>SLA tracking</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Records that a study was received.
        /// </summary>
        /// <param name="studyInstanceUid">Study UID.</param>
        /// <param name="sizeBytes">Study size in bytes.</param>
        /// <param name="instanceCount">Number of instances.</param>
        void RecordStudyReceived(string studyInstanceUid, long sizeBytes, int instanceCount);

        /// <summary>
        /// Records the start of a transfer operation.
        /// </summary>
        /// <param name="studyInstanceUid">Study UID.</param>
        /// <param name="destination">Destination identifier.</param>
        void RecordTransferStarted(string studyInstanceUid, string destination);

        /// <summary>
        /// Records the completion of a transfer operation.
        /// </summary>
        /// <param name="studyInstanceUid">Study UID.</param>
        /// <param name="duration">Transfer duration.</param>
        /// <param name="success">Whether transfer succeeded.</param>
        void RecordTransferCompleted(string studyInstanceUid, TimeSpan duration, bool success);

        /// <summary>
        /// Records an error occurrence.
        /// </summary>
        /// <param name="errorType">Type/category of error.</param>
        /// <param name="message">Error message.</param>
        /// <param name="severity">Severity level.</param>
        void RecordError(string errorType, string message, int severity = 2);

        /// <summary>
        /// Records a DICOM association event.
        /// </summary>
        /// <param name="callingAeTitle">Source AE Title.</param>
        /// <param name="action">Association action.</param>
        /// <param name="success">Whether successful.</param>
        void RecordAssociation(string callingAeTitle, string action, bool success);

        /// <summary>
        /// Records current queue depth.
        /// </summary>
        /// <param name="queueName">Queue identifier.</param>
        /// <param name="depth">Current queue size.</param>
        void RecordQueueDepth(string queueName, int depth);

        /// <summary>
        /// Records storage usage.
        /// </summary>
        /// <param name="availableMb">Available storage in MB.</param>
        /// <param name="totalMb">Total storage capacity in MB.</param>
        void RecordStorageUsage(long availableMb, long totalMb);

        /// <summary>
        /// Records system resource usage.
        /// </summary>
        /// <param name="cpuPercent">CPU usage percentage.</param>
        /// <param name="memoryMb">Memory usage in MB.</param>
        void RecordResourceUsage(double cpuPercent, double memoryMb);

        /// <summary>
        /// Records a custom metric.
        /// </summary>
        /// <param name="name">Metric name.</param>
        /// <param name="value">Metric value.</param>
        /// <param name="tags">Optional tags for categorization.</param>
        void RecordMetric(string name, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        /// <param name="name">Counter name.</param>
        /// <param name="increment">Amount to increment (default 1).</param>
        /// <param name="tags">Optional tags.</param>
        void IncrementCounter(string name, int increment = 1, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Flushes pending metrics to storage.
        /// </summary>
        Task FlushAsync(CancellationToken cancellationToken = default);
    }
}
