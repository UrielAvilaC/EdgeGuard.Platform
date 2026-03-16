using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Models.Dicom
{
    /// <summary>
    /// Represents a DICOM association connection between a remote SCU (modality) and this SCP (Edge Node).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A DICOM association is a network connection established using the DICOM Upper Layer Protocol.
    /// This class tracks the lifecycle of each association for auditing, monitoring, and troubleshooting.
    /// </para>
    /// <para>
    /// <strong>Key Use Cases:</strong>
    /// <list type="bullet">
    ///   <item><description><strong>Security Auditing:</strong> Track which modalities connect and when</description></item>
    ///   <item><description><strong>Connection Monitoring:</strong> Identify connection patterns and failures</description></item>
    ///   <item><description><strong>Performance Analysis:</strong> Measure transfer rates and connection duration</description></item>
    ///   <item><description><strong>Troubleshooting:</strong> Diagnose connection issues with detailed logs</description></item>
    ///   <item><description><strong>Compliance:</strong> Maintain audit trail for regulatory requirements (HIPAA, etc.)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Lifecycle Example:</strong>
    /// <code>
    /// // 1. Association requested
    /// var association = new DicomAssociation
    /// {
    ///     CallingAeTitle = "CT_SCANNER",
    ///     CalledAeTitle = "EDGE_NODE",
    ///     RemoteIpAddress = "192.168.1.100",
    ///     Status = AssociationStatus.Requested,
    ///     ConnectedAt = DateTime.UtcNow
    /// };
    /// 
    /// // 2. Association accepted
    /// association.Status = AssociationStatus.Accepted;
    /// 
    /// // 3. Images transferred
    /// association.Status = AssociationStatus.Active;
    /// association.ImagesReceived = 150;
    /// association.BytesReceived = 1024 * 1024 * 500; // 500 MB
    /// 
    /// // 4. Association completed
    /// association.Status = AssociationStatus.Completed;
    /// association.DisconnectedAt = DateTime.UtcNow;
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Database Persistence:</strong>
    /// Store associations for:
    /// <list type="bullet">
    ///   <item><description>Real-time monitoring dashboards</description></item>
    ///   <item><description>Historical analysis and reporting</description></item>
    ///   <item><description>Security incident investigation</description></item>
    ///   <item><description>Performance trending</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DicomAssociation
    {
        /// <summary>
        /// Gets or sets the unique identifier for this association.
        /// </summary>
        /// <remarks>
        /// Auto-generated GUID for tracking. Use this as the primary key in databases.
        /// </remarks>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the Application Entity (AE) title of the calling system (SCU/modality).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The AE title identifies the remote DICOM application initiating the connection.
        /// Maximum length: 16 characters (DICOM standard).
        /// </para>
        /// <para>
        /// <strong>Examples:</strong>
        /// <list type="bullet">
        ///   <item><description>CT_SCANNER - CT modality</description></item>
        ///   <item><description>MR_UNIT_1 - MRI system</description></item>
        ///   <item><description>PACS_VIEWER - Diagnostic workstation</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string CallingAeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the Application Entity (AE) title of the called system (SCP/this node).
        /// </summary>
        /// <remarks>
        /// The AE title this Edge Node is configured to accept connections on.
        /// Should match the AE title configured in modalities.
        /// </remarks>
        public string CalledAeTitle { get; set; } = default!;

        /// <summary>
        /// Gets or sets the IP address of the remote system.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used for:
        /// <list type="bullet">
        ///   <item><description>Security validation (IP whitelisting)</description></item>
        ///   <item><description>Network troubleshooting</description></item>
        ///   <item><description>Audit trail compliance</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Format: IPv4 (e.g., "192.168.1.100") or IPv6 (e.g., "2001:db8::1")
        /// </para>
        /// </remarks>
        public string RemoteIpAddress { get; set; } = default!;

        /// <summary>
        /// Gets or sets the TCP port number of the remote system.
        /// </summary>
        /// <remarks>
        /// Typically the source port assigned by the operating system (ephemeral port).
        /// Useful for network-level diagnostics.
        /// </remarks>
        public int RemotePort { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the association was established.
        /// </summary>
        /// <remarks>
        /// Set when the A-ASSOCIATE-RQ is received. Use UTC for timezone-independent storage.
        /// </remarks>
        public DateTime ConnectedAt { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the association was closed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Null while the association is active.
        /// Set when the connection closes (normally or abnormally).
        /// </para>
        /// <para>
        /// Calculate connection duration: <c>DisconnectedAt - ConnectedAt</c>
        /// </para>
        /// </remarks>
        public DateTime? DisconnectedAt { get; set; }

        /// <summary>
        /// Gets or sets the current status of the DICOM association.
        /// </summary>
        /// <remarks>
        /// See <see cref="AssociationStatus"/> for detailed status descriptions and transitions.
        /// </remarks>
        public AssociationStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the number of DICOM images (instances) successfully received during this association.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Incremented each time a C-STORE operation completes successfully.
        /// Does not include failed transfers.
        /// </para>
        /// <para>
        /// Use for:
        /// <list type="bullet">
        ///   <item><description>Transfer progress monitoring</description></item>
        ///   <item><description>Performance metrics (images/second)</description></item>
        ///   <item><description>Validation against expected study size</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public int ImagesReceived { get; set; }

        /// <summary>
        /// Gets or sets the total number of bytes received during this association.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Includes all DICOM data transferred (images, metadata, etc.).
        /// Use for bandwidth and storage capacity planning.
        /// </para>
        /// <para>
        /// <strong>Performance Metrics:</strong>
        /// <code>
        /// var duration = (DisconnectedAt - ConnectedAt).TotalSeconds;
        /// var throughputMbps = (BytesReceived * 8 / 1_000_000) / duration;
        /// </code>
        /// </para>
        /// </remarks>
        public long BytesReceived { get; set; }

        /// <summary>
        /// Gets or sets the reason for rejection or abortion of the association.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Null for successful connections.
        /// Contains detailed error information when Status is Rejected or Aborted.
        /// </para>
        /// <para>
        /// <strong>Example Rejection Reasons:</strong>
        /// <list type="bullet">
        ///   <item><description>"Calling AE Title 'UNKNOWN_CT' not authorized"</description></item>
        ///   <item><description>"No acceptable presentation contexts"</description></item>
        ///   <item><description>"Maximum concurrent connections exceeded (5/5)"</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Example Abort Reasons:</strong>
        /// <list type="bullet">
        ///   <item><description>"Network connection lost during transfer"</description></item>
        ///   <item><description>"Protocol error: Invalid PDU type received"</description></item>
        ///   <item><description>"Timeout: No response after 60 seconds"</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Gets or sets the ID of the Edge Node that handled this association.
        /// </summary>
        /// <remarks>
        /// Useful in multi-node deployments to identify which node processed the connection.
        /// </remarks>
        public string? EdgeNodeId { get; set; }

        /// <summary>
        /// Gets or sets the list of presentation contexts accepted during association negotiation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Contains the SOP Class UIDs and transfer syntaxes negotiated.
        /// Stored as comma-separated string or JSON for database compatibility.
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// <code>
        /// "1.2.840.10008.5.1.4.1.1.2 (CT Image Storage),
        ///  1.2.840.10008.5.1.4.1.1.4 (MR Image Storage)"
        /// </code>
        /// </para>
        /// </remarks>
        public string? AcceptedPresentationContexts { get; set; }
    }
}
