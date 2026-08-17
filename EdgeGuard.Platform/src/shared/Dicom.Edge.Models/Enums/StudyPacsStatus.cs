namespace Dicom.Edge.Models.Enums
{
    /// <summary>
    /// Progress of a study through the PACS-send pipeline, tracked independently from the
    /// clinical lifecycle in <see cref="StudyStatus"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two used to share a single <c>Status</c> field, which meant one axis overwrote the
    /// other: a study that reached the PACS could no longer advance to
    /// <see cref="StudyStatus.WaitingForReport"/> or <see cref="StudyStatus.Finalized"/> when its
    /// results arrived, so the notification rules bound to those statuses never fired.
    /// </para>
    /// <para>
    /// Hub-side only. The Edge Node keeps using <see cref="StudyStatus"/> for its own queue,
    /// where there is no clinical axis to separate.
    /// </para>
    /// </remarks>
    public enum StudyPacsStatus
    {
        /// <summary>No send has been requested yet.</summary>
        NotQueued,

        /// <summary>Queued for the node to send (includes a manual resend request).</summary>
        Queued,

        /// <summary>The node reported it is transmitting.</summary>
        Sending,

        /// <summary>At least one PACS destination accepted the study.</summary>
        Sent,

        /// <summary>Every destination rejected it, or the node reported a failure.</summary>
        Failed
    }
}
