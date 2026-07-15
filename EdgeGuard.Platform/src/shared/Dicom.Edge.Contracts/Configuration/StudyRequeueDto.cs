namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Payload sent from Hub → Node to manually re-send a study to specific PACS
/// destinations. Used by the "Reenviar" action on a <c>Failed</c> study in the Hub.
/// </summary>
public sealed record StudyRequeueRequest
{
    /// <summary>DICOM Study Instance UID to re-send.</summary>
    public required string StudyInstanceUid { get; init; }

    /// <summary>
    /// <c>NodePacsServer.Id</c> values (same as Hub-side <c>PacsServer.Id</c>) to send to.
    /// Bypasses routing rules — the study is sent only to these destinations.
    /// </summary>
    public required IReadOnlyList<string> PacsIds { get; init; }
}

/// <summary>
/// Response returned by the node after accepting a manual requeue request.
/// </summary>
public sealed record StudyRequeueResponse
{
    public bool Accepted        { get; init; }
    public int  TargetCount     { get; init; }
    public DateTime AppliedAtUtc { get; init; }
    public string? Error        { get; init; }
}
