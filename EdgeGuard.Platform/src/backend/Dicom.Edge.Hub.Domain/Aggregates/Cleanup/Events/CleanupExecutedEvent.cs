using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Cleanup.Events;

public sealed record CleanupExecutedEvent(
    string PolicyId,
    ModalityType Modality,
    int StudiesDeleted,
    int Errors) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
