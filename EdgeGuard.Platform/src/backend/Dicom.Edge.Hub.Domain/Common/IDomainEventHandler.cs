namespace Dicom.Edge.Hub.Domain.Common;

/// <summary>
/// Contract for handlers that process domain events dispatched
/// after SaveChanges completes.
/// Register implementations in DI to receive events automatically.
/// </summary>
public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
