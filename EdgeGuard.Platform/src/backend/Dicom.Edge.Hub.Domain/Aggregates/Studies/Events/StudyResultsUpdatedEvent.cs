using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

/// <summary>
/// Raised when an ORU brings results for a study whose recomputed clinical status did not
/// change — typically a corrected report or an updated image link on an already-finalized study.
///
/// <para>Without it those re-sends are invisible to subscribers: <c>RecomputeCompletion</c>
/// returns early when the derived status equals the current one, so no
/// <see cref="StudyStatusChangedEvent"/> or <see cref="StudyFinalizedEvent"/> is produced and the
/// auto-send rules never see the new results.</para>
///
/// <para>Carries the artifacts present after the update so subscribers can map it to the same
/// clinical statuses a first-time arrival would have produced.</para>
/// </summary>
public sealed record StudyResultsUpdatedEvent(
    string StudyId,
    string StudyInstanceUid,
    bool HasImageLinks,
    bool HasReport) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
