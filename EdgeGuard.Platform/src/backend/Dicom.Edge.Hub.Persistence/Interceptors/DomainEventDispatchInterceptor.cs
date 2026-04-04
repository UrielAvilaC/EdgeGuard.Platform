using Dicom.Edge.Hub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Persistence.Interceptors;

/// <summary>
/// Dispatches domain events from aggregate roots after SaveChanges completes.
/// Resolves handlers via <see cref="IServiceProvider"/> to stay decoupled from
/// any specific messaging library.
/// </summary>
public class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatchInterceptor(
        ILogger<DomainEventDispatchInterceptor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var entitiesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0)
            return;

        var handlers = _serviceProvider.GetServices<IDomainEventHandler>().ToList();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                _logger.LogDebug(
                    "Dispatching domain event {EventType} (OccurredAt={OccurredAtUtc:O})",
                    domainEvent.GetType().Name,
                    domainEvent.OccurredAtUtc);

                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler.HandleAsync(domainEvent, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Handler {HandlerType} failed for domain event {EventType}",
                            handler.GetType().Name,
                            domainEvent.GetType().Name);
                    }
                }
            }
        }
    }
}
