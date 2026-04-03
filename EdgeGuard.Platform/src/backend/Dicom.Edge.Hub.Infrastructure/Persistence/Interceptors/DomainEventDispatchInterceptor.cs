using Dicom.Edge.Hub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Clears domain events from aggregates after SaveChanges completes.
/// Currently logs dispatched events. Wire to MediatR or IEventBus for actual dispatch.
/// </summary>
public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    public DomainEventDispatchInterceptor(ILogger<DomainEventDispatchInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private Task DispatchDomainEventsAsync(DbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                _logger.LogDebug(
                    "Domain event dispatched: {EventType}",
                    domainEvent.GetType().Name);
            }
        }

        return Task.CompletedTask;
    }
}
