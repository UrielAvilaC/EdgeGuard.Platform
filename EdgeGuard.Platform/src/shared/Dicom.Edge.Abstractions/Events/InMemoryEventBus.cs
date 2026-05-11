using Dicom.Edge.Abstractions.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dicom.Edge.Abstractions.Events
{
    /// <summary>
    /// In-memory implementation of <see cref="IEventBus"/> for publishing and handling domain events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation provides a thread-safe, high-performance event bus suitable for single-node deployments.
    /// Events are processed in-memory and do not persist across application restarts.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    ///   <item><description>Thread-safe using <see cref="ConcurrentDictionary{TKey, TValue}"/> and locks</description></item>
    ///   <item><description>Supports multiple subscribers per event type</description></item>
    ///   <item><description>Concurrent handler execution (all handlers run in parallel)</description></item>
    ///   <item><description>Error isolation - one handler failure doesn't affect others</description></item>
    ///   <item><description>Comprehensive logging for debugging and monitoring</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class InMemoryEventBus : IEventBus
    {
        private readonly ILogger<InMemoryEventBus> _logger;
        
        /// <summary>
        /// Stores event handlers grouped by event type.
        /// </summary>
        private readonly ConcurrentDictionary<Type, List<object>> _handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
        /// </summary>
        /// <param name="logger">Logger for event bus operations and diagnostics.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
        public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _handlers = new ConcurrentDictionary<Type, List<object>>();
        }

        /// <inheritdoc />
        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IEdgeEvent
        {
            if (@event is null)
            {
                throw new ArgumentNullException(nameof(@event));
            }

            var eventType = typeof(TEvent);
            
            _logger.LogInformation(
                "Publishing event {EventType} with ID {EventId} from source {Source}",
                @event.EventType,
                @event.EventId,
                @event.Source
            );

            if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                _logger.LogWarning(
                    "No handlers registered for event type {EventType}. Event {EventId} will not be processed.",
                    eventType.Name,
                    @event.EventId
                );
                return;
            }

            var handlersSnapshot = handlers.ToList();
            var tasks = new List<Task>(handlersSnapshot.Count);

            foreach (var handler in handlersSnapshot)
            {
                if (handler is IEventHandler<TEvent> typedHandler)
                {
                    tasks.Add(ExecuteHandlerAsync(typedHandler, @event, cancellationToken));
                }
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "Event {EventType} with ID {EventId} processed by {HandlerCount} handler(s)",
                @event.EventType,
                @event.EventId,
                tasks.Count
            );
        }

        /// <inheritdoc />
        public void Subscribe<TEvent>(IEventHandler<TEvent> handler) 
            where TEvent : IEdgeEvent
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            
            _handlers.AddOrUpdate(
                eventType,
                _ =>
                {
                    _logger.LogDebug("Creating new handler list for event type {EventType}", eventType.Name);
                    return new List<object> { handler };
                },
                (_, existingHandlers) =>
                {
                    lock (existingHandlers)
                    {
                        if (!existingHandlers.Contains(handler))
                        {
                            existingHandlers.Add(handler);
                            _logger.LogDebug(
                                "Subscribed handler {HandlerType} to event type {EventType}. Total handlers: {HandlerCount}",
                                handler.GetType().Name,
                                eventType.Name,
                                existingHandlers.Count
                            );
                        }
                    }
                    return existingHandlers;
                }
            );
        }

        /// <inheritdoc />
        public void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) 
            where TEvent : IEdgeEvent
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);

            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                lock (handlers)
                {
                    if (handlers.Remove(handler))
                    {
                        _logger.LogDebug(
                            "Unsubscribed handler {HandlerType} from event type {EventType}. Remaining handlers: {HandlerCount}",
                            handler.GetType().Name,
                            eventType.Name,
                            handlers.Count
                        );
                    }
                }
            }
        }

        private async Task ExecuteHandlerAsync<TEvent>(
            IEventHandler<TEvent> handler,
            TEvent @event,
            CancellationToken cancellationToken) 
            where TEvent : IEdgeEvent
        {
            var handlerType = handler.GetType().Name;

            try
            {
                _logger.LogDebug(
                    "Executing handler {HandlerType} for event {EventType} with ID {EventId}",
                    handlerType,
                    @event.EventType,
                    @event.EventId
                );

                await handler.HandleAsync(@event, cancellationToken);

                _logger.LogDebug(
                    "Handler {HandlerType} completed successfully for event {EventId}",
                    handlerType,
                    @event.EventId
                );
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Handler {HandlerType} was cancelled for event {EventId}",
                    handlerType,
                    @event.EventId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Handler {HandlerType} failed while processing event {EventType} with ID {EventId}. Error: {ErrorMessage}",
                    handlerType,
                    @event.EventType,
                    @event.EventId,
                    ex.Message
                );
            }
        }
    }
}
