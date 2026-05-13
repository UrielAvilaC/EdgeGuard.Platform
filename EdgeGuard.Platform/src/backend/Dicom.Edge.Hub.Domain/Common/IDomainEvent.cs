namespace Dicom.Edge.Hub.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregates.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
