using Dicom.Edge.Common.Results;

namespace Dicom.Edge.Abstractions.Queue
{
    /// <summary>
    /// Defines a queue for managing asynchronous work items with priority and retry support.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    /// <remarks>
    /// <para>
    /// The queue supports:
    /// <list type="bullet">
    ///   <item><description>Priority-based processing</description></item>
    ///   <item><description>Retry with exponential backoff</description></item>
    ///   <item><description>Peek without removing</description></item>
    ///   <item><description>Batch operations</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IEdgeQueue<T>
    {
        // ==================== Basic Operations ====================

        /// <summary>
        /// Adds an item to the queue with optional priority.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /// <param name="priority">Priority level (lower = higher priority, default = 5).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> EnqueueAsync(T item, int priority = 5, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple items to the queue.
        /// </summary>
        /// <param name="items">Items to enqueue.</param>
        /// <param name="priority">Priority for all items.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> EnqueueBatchAsync(
            IEnumerable<T> items,
            int priority = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes and returns the highest priority item from the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The dequeued item, or null if queue is empty.</returns>
        Task<Result<T?>> DequeueAsync(CancellationToken cancellationToken = default);

        // ==================== Inspection ====================

        /// <summary>
        /// Peeks at the next item without removing it.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The next item, or null if queue is empty.</returns>
        Task<Result<T?>> PeekAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current number of items in the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<int>> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves pending items without removing them (for monitoring/dashboards).
        /// </summary>
        /// <param name="count">Maximum number of items to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<IEnumerable<T>>> GetPendingAsync(int count = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the queue is empty.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result<bool>> IsEmptyAsync(CancellationToken cancellationToken = default);

        // ==================== Retry Management ====================

        /// <summary>
        /// Re-queues an item after a delay (for retry scenarios).
        /// </summary>
        /// <param name="item">Item to requeue.</param>
        /// <param name="delay">Delay before item becomes available again.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> RequeueAsync(T item, TimeSpan delay, CancellationToken cancellationToken = default);

        // ==================== Removal ====================

        /// <summary>
        /// Removes a specific item from the queue.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> RemoveAsync(T item, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all items from the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<Result> ClearAsync(CancellationToken cancellationToken = default);
    }
}
