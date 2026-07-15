namespace Dicom.Edge.Hub.Application.Studies;

public interface IStudyResendService
{
    /// <summary>
    /// Manually re-sends a <c>Failed</c> study to the chosen PACS destinations.
    /// </summary>
    Task<StudyResendResult> RequeueAsync(
        string studyId, IReadOnlyList<string> pacsIds, CancellationToken ct = default);
}

/// <summary>Outcome of a manual study resend request.</summary>
public sealed record StudyResendResult
{
    public bool Accepted { get; init; }
    public bool NotFound { get; init; }
    public string? Error { get; init; }

    public static StudyResendResult Ok() => new() { Accepted = true };
    public static StudyResendResult MissingStudy() => new() { NotFound = true };
    public static StudyResendResult Invalid(string error) => new() { Error = error };
}
