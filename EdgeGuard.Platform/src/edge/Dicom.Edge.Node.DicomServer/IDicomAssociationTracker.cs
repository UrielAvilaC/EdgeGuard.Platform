namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Tracks the full lifecycle of DICOM associations for auditing and monitoring.
/// fo-dicom creates one <see cref="CStoreScp"/> instance per association, so the
/// tracker returns a per-association <see cref="IAssociationSession"/> that is held
/// as an instance field and finalized on release, abort, or unexpected close.
/// </summary>
public interface IDicomAssociationTracker
{
    /// <summary>
    /// Opens a tracking session for an accepted association.
    /// The returned session accumulates per-image counters and is persisted on finalization.
    /// </summary>
    Task<IAssociationSession> BeginAsync(
        string callingAe,
        string calledAe,
        string remoteHost,
        int    remotePort,
        string? acceptedContexts,
        CancellationToken ct = default);

    /// <summary>
    /// Persists a rejected association in a single call. No session is required.
    /// </summary>
    Task RecordRejectionAsync(
        string callingAe,
        string calledAe,
        string remoteHost,
        int    remotePort,
        string reason,
        CancellationToken ct = default);
}

/// <summary>
/// Per-association tracking handle returned by <see cref="IDicomAssociationTracker.BeginAsync"/>.
/// All members are safe to call from concurrent threads.
/// </summary>
public interface IAssociationSession
{
    /// <summary>Increments the received-image counter (thread-safe).</summary>
    void RecordImage();

    /// <summary>Persists the association record with status <c>Completed</c>.</summary>
    Task CompleteAsync(CancellationToken ct = default);

    /// <summary>Persists the association record with status <c>Aborted</c> and the supplied reason.</summary>
    Task AbortAsync(string? reason, CancellationToken ct = default);
}
