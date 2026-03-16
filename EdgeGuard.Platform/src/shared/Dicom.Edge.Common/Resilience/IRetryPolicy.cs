using System.Net.Sockets;

namespace Dicom.Edge.Common.Resilience
{
    /// <summary>
    /// Provides retry logic with exponential backoff for transient failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Retry policies are useful for:
    /// <list type="bullet">
    ///   <item><description>Network operations (HTTP, DICOM transfers)</description></item>
    ///   <item><description>Database deadlocks</description></item>
    ///   <item><description>External service timeouts</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var policy = new RetryPolicy(maxRetries: 3, initialDelaySeconds: 1);
    /// var result = await policy.ExecuteAsync(async () =>
    /// {
    ///     return await httpClient.PostAsync(url, content);
    /// });
    /// </code>
    /// </para>
    /// </remarks>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation without return value with retry logic.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExecuteAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of retry policy with exponential backoff.
    /// </summary>
    public class RetryPolicy : IRetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialDelaySeconds;
        private readonly int _maxDelaySeconds;
        private readonly bool _useExponentialBackoff;

        public RetryPolicy(
            int maxRetries = 3,
            int initialDelaySeconds = 1,
            int maxDelaySeconds = 60,
            bool useExponentialBackoff = true)
        {
            _maxRetries = maxRetries;
            _initialDelaySeconds = initialDelaySeconds;
            _maxDelaySeconds = maxDelaySeconds;
            _useExponentialBackoff = useExponentialBackoff;
        }

        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (attempt < _maxRetries && IsTransient(ex))
                {
                    attempt++;
                    var delay = CalculateDelay(attempt);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        public async Task ExecuteAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }

        private TimeSpan CalculateDelay(int attempt)
        {
            var delay = _useExponentialBackoff
                ? _initialDelaySeconds * Math.Pow(2, attempt - 1)
                : _initialDelaySeconds;

            return TimeSpan.FromSeconds(Math.Min(delay, _maxDelaySeconds));
        }

        private static bool IsTransient(Exception ex)
        {
            // Common transient exceptions
            return ex is TimeoutException
                || ex is HttpRequestException
                || ex is IOException
                || ex.InnerException is SocketException;
        }
    }
}
