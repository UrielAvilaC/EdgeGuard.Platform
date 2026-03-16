using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dicom.Edge.Abstractions.Events
{
    /// <summary>
    /// Defines a contract for publishing and subscribing to domain events within the EdgeGuard platform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event bus provides a decoupled communication mechanism between different components of the system.
    /// It follows the publish-subscribe pattern where publishers emit events without knowledge of subscribers,
    /// and subscribers handle events without direct coupling to publishers.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> Implementations must be thread-safe to support concurrent publishing and subscription.
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong> Implementations should ensure that failures in one handler do not affect others.
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// // Publishing an event
    /// var studyContext = new StudyContext { StudyInstanceUid = "1.2.3.4" };
    /// var @event = new StudyCompletedEvent(studyContext);
    /// await eventBus.PublishAsync(@event, cancellationToken);
    /// 
    /// // Subscribing to an event
    /// var handler = new StudyCompletedEventHandler(logger);
    /// eventBus.Subscribe&lt;StudyCompletedEvent&gt;(handler);
    /// </code>
    /// </para>
    /// </remarks>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an event to all registered handlers asynchronously.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to publish. Must implement <see cref="IEdgeEvent"/>.</typeparam>
        /// <param name="event">The event instance to publish. Cannot be null.</param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous publish operation. The task completes when all handlers have finished processing.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is null.</exception>
        /// <remarks>
        /// All registered handlers for the event type will be invoked concurrently. If no handlers are registered,
        /// the method completes without error. Handler exceptions are logged but do not prevent other handlers from executing.
        /// </remarks>
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IEdgeEvent;

        /// <summary>
        /// Subscribes a handler to a specific event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to subscribe to. Must implement <see cref="IEdgeEvent"/>.</typeparam>
        /// <param name="handler">The handler to invoke when the event is published. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// The same handler instance can only be subscribed once per event type. Subsequent subscriptions of the same
        /// instance are ignored.
        /// </para>
        /// <para>
        /// Handlers are invoked in the order they were subscribed, though this should not be relied upon for correctness.
        /// </para>
        /// </remarks>
        void Subscribe<TEvent>(IEventHandler<TEvent> handler) 
            where TEvent : IEdgeEvent;

        /// <summary>
        /// Unsubscribes a handler from a specific event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of event to unsubscribe from. Must implement <see cref="IEdgeEvent"/>.</typeparam>
        /// <param name="handler">The handler to remove. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler"/> is null.</exception>
        /// <remarks>
        /// If the handler is not currently subscribed, this method has no effect and completes without error.
        /// After unsubscription, the handler will no longer receive events of the specified type.
        /// </remarks>
        void Unsubscribe<TEvent>(IEventHandler<TEvent> handler) 
            where TEvent : IEdgeEvent;
    }
}
