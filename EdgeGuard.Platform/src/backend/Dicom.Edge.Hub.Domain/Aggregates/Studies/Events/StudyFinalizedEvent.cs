using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;

/// <summary>
/// Raised when a study becomes <c>Finalized</c> — i.e. it has BOTH the image link
/// (liga de imágenes) and the diagnostic report. Triggers automatic results delivery
/// when the notifications auto-mode is enabled.
/// </summary>
public sealed record StudyFinalizedEvent(
    string StudyId,
    string StudyInstanceUid,
    bool HasImageLinks,
    bool HasReport) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
