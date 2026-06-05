using Dicom.Edge.Node.Sender;

namespace Dicom.Edge.Node.Router;

/// <summary>
/// Determines the PACS destination for a study based on routing rules.
/// </summary>
public interface IStudyRouter
{
    /// <summary>
    /// Resolves the PACS destination(s) for a completed study.
    /// </summary>
    Task<IReadOnlyList<PacsDestination>> ResolveDestinationsAsync(
        StudyRoutingContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Context data for making routing decisions.
/// </summary>
public sealed class StudyRoutingContext
{
    public required string StudyInstanceUid { get; init; }
    public string? Modality { get; init; }
    public string? SourceAeTitle { get; init; }
    public string? InstitutionName { get; init; }
    public string? ReferringPhysician { get; init; }
    public string? StudyDescription { get; init; }
    public string? AccessionNumber { get; init; }
    public int InstanceCount { get; init; }
    public int Priority { get; init; } = 5;
    public bool IsUrgent { get; init; }
}
