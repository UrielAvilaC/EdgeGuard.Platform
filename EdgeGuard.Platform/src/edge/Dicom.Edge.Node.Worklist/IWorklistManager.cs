using Dicom.Edge.Contracts.Hl7;

namespace Dicom.Edge.Node.Worklist;

/// <summary>
/// Manages HL7 worklist items received from the Hub.
/// Stores them locally and provides them for MWL queries from modalities.
/// </summary>
public interface IWorklistManager
{
    /// <summary>
    /// Accepts a worklist push from the Hub and stores it locally.
    /// </summary>
    Task<WorklistAcceptResult> AcceptWorklistItemAsync(
        Hl7WorklistPushRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all active (non-expired) worklist items for a modality.
    /// </summary>
    Task<IReadOnlyList<WorklistItem>> GetActiveItemsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets worklist items matching a date range and optional modality filter.
    /// </summary>
    Task<IReadOnlyList<WorklistItem>> QueryAsync(
        DateTime? from, DateTime? to, string? modality,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the total count of active worklist items.
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks worklist items as queried by a modality so they are not returned again.
    /// </summary>
    Task MarkItemsAsQueriedAsync(IEnumerable<string> ids, CancellationToken ct = default);
}

public sealed class WorklistAcceptResult
{
    public required bool Accepted { get; init; }
    public string? AckId { get; init; }
    public string? Error { get; init; }
}
