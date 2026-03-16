namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Operational status of an Edge Node.
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// Node is running normally and accepting connections.
        /// </summary>
        Online = 0,
        
        /// <summary>
        /// Node is not responding or unreachable.
        /// </summary>
        Offline = 1,
        
        /// <summary>
        /// Node is running but experiencing issues (high error rate, disk full, etc.).
        /// </summary>
        Degraded = 2,
        
        /// <summary>
        /// Node is in maintenance mode (not accepting new studies).
        /// </summary>
        Maintenance = 3,
        
        /// <summary>
        /// Node is starting up.
        /// </summary>
        Starting = 4,
        
        /// <summary>
        /// Node is shutting down gracefully.
        /// </summary>
        Stopping = 5
    }
}
