using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks.Events;

public sealed record HealthCheckReceivedEvent(
    string NodeId,
    string HealthCheckRecordId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
