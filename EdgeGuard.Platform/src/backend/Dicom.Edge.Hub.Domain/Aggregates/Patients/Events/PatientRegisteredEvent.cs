using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Patients.Events;

public sealed record PatientRegisteredEvent(
    string PatientId,
    string PatientDicomId,
    string PatientName,
    string? NodeId) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
