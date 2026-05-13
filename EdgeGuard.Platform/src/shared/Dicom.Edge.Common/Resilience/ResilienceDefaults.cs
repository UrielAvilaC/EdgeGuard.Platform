namespace Dicom.Edge.Common.Resilience;

/// <summary>
/// Default resilience settings used across the platform.
/// Configurable via <c>Resilience</c> section in appsettings.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Resilience";

    /// <summary>Maximum number of retry attempts for transient failures.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Median delay in seconds for the first retry (jittered exponential backoff).</summary>
    public double RetryBaseDelaySeconds { get; set; } = 1;

    /// <summary>Maximum delay in seconds between retries.</summary>
    public double RetryMaxDelaySeconds { get; set; } = 30;

    /// <summary>Minimum throughput before the circuit breaker evaluates the failure ratio.</summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>Failure ratio (0.0–1.0) that triggers the circuit to open.</summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>Sampling window in seconds used to evaluate the failure ratio.</summary>
    public double CircuitBreakerSamplingDurationSeconds { get; set; } = 30;

    /// <summary>Duration in seconds the circuit stays open before transitioning to half-open.</summary>
    public double CircuitBreakerBreakDurationSeconds { get; set; } = 30;

    /// <summary>Timeout in seconds for individual operations. 0 disables the timeout.</summary>
    public double TimeoutSeconds { get; set; } = 30;
}
