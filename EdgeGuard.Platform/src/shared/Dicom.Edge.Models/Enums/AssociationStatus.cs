namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Represents the current status of a DICOM association connection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// DICOM associations follow a lifecycle: Request → Accept/Reject → Active → Completed/Aborted.
    /// This enum tracks the current state of the association for monitoring and auditing purposes.
    /// </para>
    /// <para>
    /// <strong>Typical Flow:</strong>
    /// <code>
    /// Requested → Accepted → Active → Completed
    /// </code>
    /// </para>
    /// <para>
    /// <strong>Error Flow:</strong>
    /// <code>
    /// Requested → Rejected (invalid credentials, unsupported SOP classes, etc.)
    /// Active → Aborted (network failure, timeout, protocol error)
    /// </code>
    /// </para>
    /// </remarks>
    public enum AssociationStatus
    {
        /// <summary>
        /// Association has been requested by a remote SCU but not yet processed.
        /// </summary>
        /// <remarks>
        /// Initial state when a DICOM A-ASSOCIATE-RQ is received.
        /// Transitions to Accepted or Rejected based on validation.
        /// </remarks>
        Requested = 0,

        /// <summary>
        /// Association has been accepted and is being established.
        /// </summary>
        /// <remarks>
        /// The SCP has validated the request and sent an A-ASSOCIATE-AC response.
        /// Transitions to Active once the connection is fully established.
        /// </remarks>
        Accepted = 1,

        /// <summary>
        /// Association was rejected by the SCP.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Common rejection reasons:
        /// <list type="bullet">
        ///   <item><description>Invalid or unauthorized calling AE title</description></item>
        ///   <item><description>Unsupported presentation contexts</description></item>
        ///   <item><description>Protocol version mismatch</description></item>
        ///   <item><description>Resource limitations (max connections reached)</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Terminal state - no further transitions.
        /// </para>
        /// </remarks>
        Rejected = 2,

        /// <summary>
        /// Association is active and ready for data transfer.
        /// </summary>
        /// <remarks>
        /// C-STORE, C-FIND, and other DICOM services can be performed.
        /// Transitions to Completed (normal closure) or Aborted (error).
        /// </remarks>
        Active = 3,

        /// <summary>
        /// Association completed normally and was released gracefully.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Occurs when:
        /// <list type="bullet">
        ///   <item><description>The remote SCU sends A-RELEASE-RQ</description></item>
        ///   <item><description>All pending operations complete successfully</description></item>
        ///   <item><description>Connection is closed properly</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Terminal state - indicates successful completion.
        /// </para>
        /// </remarks>
        Completed = 4,

        /// <summary>
        /// Association was aborted due to an error or timeout.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Common abort reasons:
        /// <list type="bullet">
        ///   <item><description>Network connection lost</description></item>
        ///   <item><description>Protocol error (invalid PDU, sequence violation)</description></item>
        ///   <item><description>Timeout waiting for response</description></item>
        ///   <item><description>Application-level error during transfer</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Terminal state - indicates abnormal termination.
        /// Check RejectionReason in DicomAssociation for details.
        /// </para>
        /// </remarks>
        Aborted = 5
    }
}
