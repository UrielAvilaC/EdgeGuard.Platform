using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Identity.Events;

public sealed record UserLockedEvent(
    string UserId,
    string Username,
    DateTime LockedUntil) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
