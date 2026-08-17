using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Dicom.Edge.Common.Resilience;

/// <summary>
/// Extension methods for registering platform-standard resilience pipelines
/// backed by Polly v8 via <c>Microsoft.Extensions.Resilience</c>.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>Well-known name for the standard platform resilience pipeline.</summary>
    public const string StandardPipelineName = "edgeguard-standard";

    /// <summary>
    /// Registers a named <see cref="ResiliencePipeline"/> with retry + circuit breaker
    /// using the <see cref="ResilienceOptions"/> configuration section.
    /// </summary>
    public static IServiceCollection AddPlatformResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(
            configuration.GetSection(ResilienceOptions.SectionName));

        services.AddResiliencePipeline(StandardPipelineName, (builder, context) =>
        {
            var options = context.ServiceProvider
                .GetRequiredService<IOptions<ResilienceOptions>>().Value;

            builder
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
                    MaxDelay = TimeSpan.FromSeconds(options.RetryMaxDelaySeconds),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .Handle<IOException>()
                        .Handle<System.Net.Sockets.SocketException>()
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                    FailureRatio = options.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreakerSamplingDurationSeconds),
                    BreakDuration = TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<HttpRequestException>()
                        .Handle<TimeoutException>()
                        .Handle<IOException>()
                        .Handle<System.Net.Sockets.SocketException>()
                });

            if (options.TimeoutSeconds > 0)
            {
                builder.AddTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));
            }
        });

        return services;
    }

    /// <summary>
    /// P0-10: Registers a per-node <see cref="ResiliencePipelineRegistry{TKey}"/> keyed by
    /// node identifier. Each node gets its OWN circuit breaker so one unreachable node
    /// does not trip the breaker for all other nodes (no more cascading failure).
    ///
    /// Usage:
    /// <code>
    /// var pipeline = registry.GetPipeline&lt;HttpResponseMessage&gt;(nodeId);
    /// var response = await pipeline.ExecuteAsync(async ct =&gt; await client.SendAsync(req, ct), ct);
    /// </code>
    /// </summary>
    public static IServiceCollection AddPerNodeResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ResilienceOptions>(
            configuration.GetSection(ResilienceOptions.SectionName));

        services.AddSingleton<INodeResiliencePipelineProvider, NodeResiliencePipelineProvider>();
        return services;
    }
}
