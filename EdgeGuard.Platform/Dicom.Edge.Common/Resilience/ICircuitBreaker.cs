namespace Dicom.Edge.Common.Resilience
{
    /// <summary>
    /// Circuit breaker pattern to prevent cascading failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// States:
    /// <list type="bullet">
    ///   <item><description><strong>Closed:</strong> Normal operation</description></item>
    ///   <item><description><strong>Open:</strong> Failures exceeded threshold, reject calls</description></item>
    ///   <item><description><strong>HalfOpen:</strong> Test if service recovered</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Executes an operation through the circuit breaker.
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current circuit state.
        /// </summary>
        CircuitState State { get; }

        /// <summary>
        /// Manually resets the circuit to closed state.
        /// </summary>
        void Reset();
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Default circuit breaker implementation.
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;
        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitState _state = CircuitState.Closed;
        private readonly object _lock = new();

        public CircuitBreaker(int failureThreshold = 5, TimeSpan? openDuration = null)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
        }

        public CircuitState State
        {
            get
            {
                lock (_lock)
                {
                    if (_state == CircuitState.Open &&
                        DateTime.UtcNow - _lastFailureTime >= _openDuration)
                    {
                        _state = CircuitState.HalfOpen;
                    }
                    return _state;
                }
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (State == CircuitState.Open)
            {
                throw new InvalidOperationException("Circuit breaker is open");
            }

            try
            {
                var result = await operation();
                OnSuccess();
                return result;
            }
            catch (Exception)
            {
                OnFailure();
                throw;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
        }

        private void OnSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
            }
        }

        private void OnFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                }
            }
        }
    }
}
