using System.Threading;
using System.Threading.Tasks;

namespace Dicom.Edge.Abstractions.Events
{
    /// <summary>
    /// Defines a handler for processing specific domain event types in the EdgeGuard platform.
    /// </summary>
    /// <typeparam name="TEvent">
    /// The type of event this handler processes. Must implement <see cref="IEdgeEvent"/>.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Event handlers are invoked by the <see cref="IEventBus"/> when events are published.
    /// Handlers should be registered as services in the dependency injection container and
    /// subscribed to the event bus during application startup.
    /// </para>
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    ///   <item><description>Keep handler logic fast and lightweight</description></item>
    ///   <item><description>Avoid blocking operations; use async/await properly</description></item>
    ///   <item><description>Handle exceptions gracefully within the handler</description></item>
    ///   <item><description>Don't assume execution order when multiple handlers exist</description></item>
    ///   <item><description>Consider idempotency for retry scenarios</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Implementation Example:</strong>
    /// <code>
    /// public class StudyCompletedEventHandler : IEventHandler&lt;StudyCompletedEvent&gt;
    /// {
    ///     private readonly ILogger&lt;StudyCompletedEventHandler&gt; _logger;
    ///     private readonly IEdgeQueue&lt;StudyContext&gt; _queue;
    ///     
    ///     public StudyCompletedEventHandler(
    ///         ILogger&lt;StudyCompletedEventHandler&gt; logger,
    ///         IEdgeQueue&lt;StudyContext&gt; queue)
    ///     {
    ///         _logger = logger;
    ///         _queue = queue;
    ///     }
    ///     
    ///     public async Task HandleAsync(StudyCompletedEvent @event, CancellationToken cancellationToken)
    ///     {
    ///         _logger.LogInformation("Processing study {StudyUID}", @event.Study.StudyInstanceUid);
    ///         await _queue.Queue(@event.Study);
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IEventHandler<in TEvent> where TEvent : IEdgeEvent
    {
        /// <summary>
        /// Handles the specified event asynchronously.
        /// </summary>
        /// <param name="event">The event to handle. Will never be null.</param>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
        /// Handlers should respect cancellation and stop processing when requested.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous handling operation. The task should complete when
        /// the handler has finished processing the event.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is invoked by the event bus when an event of type <typeparamref name="TEvent"/> is published.
        /// Implementations should handle exceptions internally and log them appropriately. Throwing exceptions
        /// will be caught by the event bus, logged, but will not prevent other handlers from executing.
        /// </para>
        /// <para>
        /// If the handler needs to perform long-running operations, consider:
        /// <list type="bullet">
        ///   <item><description>Queueing work for background processing</description></item>
        ///   <item><description>Using fire-and-forget patterns with proper error handling</description></item>
        ///   <item><description>Implementing timeout mechanisms</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
