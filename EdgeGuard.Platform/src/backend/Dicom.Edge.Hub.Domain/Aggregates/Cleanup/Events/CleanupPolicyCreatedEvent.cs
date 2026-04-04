using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Cleanup.Events;

public sealed record CleanupPolicyCreatedEvent(
    string PolicyId,
    ModalityType Modality,
    int RetentionDays) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
