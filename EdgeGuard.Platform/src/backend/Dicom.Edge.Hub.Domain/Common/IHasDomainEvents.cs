namespace Dicom.Edge.Hub.Domain.Common;

/// <summary>
/// Interface for entities that can raise domain events.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
