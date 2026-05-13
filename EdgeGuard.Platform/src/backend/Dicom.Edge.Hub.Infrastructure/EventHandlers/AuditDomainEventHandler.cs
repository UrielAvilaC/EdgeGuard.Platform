using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.EventHandlers;

/// <summary>
/// Handles all domain events by writing an audit trail entry to the <see cref="HubAuditLog"/> table.
/// Registered as an <see cref="IDomainEventHandler"/> so the
/// <see cref="Dicom.Edge.Hub.Persistence.Interceptors.DomainEventDispatchInterceptor"/>
/// dispatches every persisted domain event to this handler automatically.
/// </summary>
public sealed class AuditDomainEventHandler(
    IHubAuditLogRepository auditRepository,
    ILogger<AuditDomainEventHandler> logger) : IDomainEventHandler
{
    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var eventTypeName = domainEvent.GetType().Name;

        var entry = HubAuditLog.Create(
            eventType: AuditEventType.DomainEvent,
            action: eventTypeName,
            severity: AuditSeverity.Information,
            details: $"Domain event dispatched at {domainEvent.OccurredAtUtc:O}",
            isSuccess: true);

        await auditRepository.AddAsync(entry, ct);

        logger.LogDebug("Audit logged domain event {EventType}", eventTypeName);
    }
}
