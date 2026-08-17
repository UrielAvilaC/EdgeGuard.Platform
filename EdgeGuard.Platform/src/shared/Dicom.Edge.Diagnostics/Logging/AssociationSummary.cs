namespace Dicom.Edge.Diagnostics.Logging;

/// <summary>Final outcome of a DICOM association, written in the log footer.</summary>
public enum AssociationOutcome
{
    /// <summary>Association was accepted but has not finished yet.</summary>
    Accepted,

    /// <summary>Association was rejected during negotiation.</summary>
    Rejected,

    /// <summary>Association was released normally by the SCU.</summary>
    Completed,

    /// <summary>Association was aborted (A-ABORT or connection error).</summary>
    Aborted,

    /// <summary>Connection closed without release or abort.</summary>
    Closed,

    /// <summary>No close callback arrived; the file was closed by the stale sweeper.</summary>
    Orphaned
}

/// <summary>
/// Counters accumulated during a single association and rendered in the log footer.
/// All mutating members are safe to call from concurrent callbacks.
/// </summary>
public sealed class AssociationSummary
{
    private int _cStoreOk;
    private int _cStoreFailed;
    private int _cFindMwl;
    private int _cFindQr;
    private int _cFindResults;
    private int _cEcho;
    private int _errors;
    private long _bytesReceived;

    public int CStoreOk => Volatile.Read(ref _cStoreOk);
    public int CStoreFailed => Volatile.Read(ref _cStoreFailed);
    public int CFindMwl => Volatile.Read(ref _cFindMwl);
    public int CFindQr => Volatile.Read(ref _cFindQr);
    public int CFindResults => Volatile.Read(ref _cFindResults);
    public int CEcho => Volatile.Read(ref _cEcho);
    public int Errors => Volatile.Read(ref _errors);
    public long BytesReceived => Volatile.Read(ref _bytesReceived);

    /// <summary>Final outcome; defaults to <see cref="AssociationOutcome.Closed"/>.</summary>
    public AssociationOutcome Outcome { get; set; } = AssociationOutcome.Closed;

    /// <summary>Reason recorded when the association was rejected or aborted.</summary>
    public string? Reason { get; set; }

    /// <summary>Presentation contexts accepted during negotiation.</summary>
    public string? AcceptedContexts { get; set; }

    public void RecordCStore(bool success, long bytes = 0)
    {
        if (success) Interlocked.Increment(ref _cStoreOk);
        else Interlocked.Increment(ref _cStoreFailed);

        if (bytes > 0) Interlocked.Add(ref _bytesReceived, bytes);
    }

    public void RecordMwlQuery(int results)
    {
        Interlocked.Increment(ref _cFindMwl);
        Interlocked.Add(ref _cFindResults, results);
    }

    public void RecordQrQuery(int results)
    {
        Interlocked.Increment(ref _cFindQr);
        Interlocked.Add(ref _cFindResults, results);
    }

    public void RecordCEcho() => Interlocked.Increment(ref _cEcho);

    public void RecordError() => Interlocked.Increment(ref _errors);
}
