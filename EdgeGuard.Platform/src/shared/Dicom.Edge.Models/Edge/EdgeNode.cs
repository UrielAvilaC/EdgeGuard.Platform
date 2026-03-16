using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Edge
{
    /// <summary>
    /// Represents an Edge Node registered with the central Hub.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Edge Nodes are deployed at remote sites (hospitals, clinics) to receive DICOM studies
    /// from modalities and forward them to the central Hub.
    /// </para>
    /// <para>
    /// The Hub tracks all registered nodes for:
    /// <list type="bullet">
    ///   <item><description>Health monitoring and alerting</description></item>
    ///   <item><description>Load balancing and capacity planning</description></item>
    ///   <item><description>Security and access control</description></item>
    ///   <item><description>Geographic routing decisions</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class EdgeNode
    {
        /// <summary>
        /// Gets or sets the unique identifier for this Edge Node.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the friendly name of this node.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Examples:
        /// <list type="bullet">
        ///   <item><description>"Main Hospital - Radiology"</description></item>
        ///   <item><description>"Outpatient Clinic - West Campus"</description></item>
        ///   <item><description>"Emergency Department Node"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the DICOM Application Entity Title of this node.
        /// </summary>
        public string AETitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the public IP address or hostname where this node is reachable.
        /// </summary>
        public string IpAddress { get; set; } = default!;

        /// <summary>
        /// Gets or sets the DICOM port this node listens on.
        /// </summary>
        public int Port { get; set; } = 104;

        /// <summary>
        /// Gets or sets the API endpoint URL for HTTP communication.
        /// </summary>
        /// <remarks>
        /// Example: "https://edge-node-1.hospital.com:443"
        /// </remarks>
        public string? ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the node last reported health status.
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the current operational status of the node.
        /// </summary>
        public NodeStatus Status { get; set; } = NodeStatus.Offline;

        /// <summary>
        /// Gets or sets the software version running on this node.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this node was registered.
        /// </summary>
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the physical location/address of this node.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets the facility or organization name.
        /// </summary>
        public string? FacilityName { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the node location.
        /// </summary>
        /// <remarks>
        /// Example: "America/New_York", "Europe/London"
        /// </remarks>
        public string? TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the authentication API key for this node.
        /// </summary>
        /// <remarks>
        /// Store hashed version for security. Never expose in APIs.
        /// </remarks>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets whether this node is enabled for operation.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum storage capacity in megabytes.
        /// </summary>
        public long MaxStorageMb { get; set; }

        /// <summary>
        /// Gets or sets the current available storage in megabytes.
        /// </summary>
        /// <remarks>
        /// Updated periodically by node health reports.
        /// </remarks>
        public long AvailableStorageMb { get; set; }

        /// <summary>
        /// Gets or sets the number of studies currently queued on this node.
        /// </summary>
        public int QueuedStudies { get; set; }

        /// <summary>
        /// Gets or sets the total number of studies received by this node (lifetime).
        /// </summary>
        public int TotalStudiesReceived { get; set; }

        /// <summary>
        /// Gets or sets the total number of studies successfully sent to Hub.
        /// </summary>
        public int TotalStudiesSent { get; set; }

        /// <summary>
        /// Gets or sets the number of errors in the last 24 hours.
        /// </summary>
        public int ErrorsLast24Hours { get; set; }

        /// <summary>
        /// Gets or sets contact information for this node's administrator.
        /// </summary>
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Gets or sets contact phone number.
        /// </summary>
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Gets or sets additional notes about this node.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets configuration overrides as JSON.
        /// </summary>
        /// <remarks>
        /// Node-specific settings that override global Hub configuration.
        /// </remarks>
        public string? ConfigurationOverrides { get; set; }

        /// <summary>
        /// Gets whether the node is currently online.
        /// </summary>
        /// <remarks>
        /// Consider online if last seen within 5 minutes.
        /// </remarks>
        public bool IsOnline => (DateTime.UtcNow - LastSeen).TotalMinutes < 5;

        /// <summary>
        /// Gets the storage usage percentage.
        /// </summary>
        public double StorageUsagePercent =>
            MaxStorageMb > 0
                ? ((MaxStorageMb - AvailableStorageMb) / (double)MaxStorageMb) * 100
                : 0;

        /// <summary>
        /// Gets the success rate of transfers.
        /// </summary>
        public double TransferSuccessRate =>
            TotalStudiesReceived > 0
                ? (TotalStudiesSent / (double)TotalStudiesReceived) * 100
                : 100;
    }
}
