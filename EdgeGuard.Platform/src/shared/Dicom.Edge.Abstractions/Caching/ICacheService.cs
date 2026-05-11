namespace Dicom.Edge.Abstractions.Caching
{
    /// <summary>
    /// Service for caching frequently accessed data to improve performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Caching reduces database load and improves response times for:
    /// <list type="bullet">
    ///   <item><description>Configuration data</description></item>
    ///   <item><description>Frequently queried studies</description></item>
    ///   <item><description>User sessions</description></item>
    ///   <item><description>Lookup data (modalities, routing rules)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICacheService
    {
        /// <summary>
        /// Retrieves a cached value.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached value if found; otherwise, null.</returns>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a value in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to cache.</param>
        /// <param name="expiration">Optional expiration time (default: no expiration).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a value from the cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in the cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if key exists; otherwise, false.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a cached value or creates it if not found.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">Cache key.</param>
        /// <param name="factory">Factory function to create value if not cached.</param>
        /// <param name="expiration">Optional expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly created value.</returns>
        Task<T> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all keys matching a pattern.
        /// </summary>
        /// <param name="pattern">Pattern to match (e.g., "study:*").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
