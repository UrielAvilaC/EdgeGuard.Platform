using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Dicom.Edge.Common.Resilience;

/// <summary>
/// Default <see cref="INodeResiliencePipelineProvider"/> backed by a
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> of per-node pipelines.
/// </summary>
public sealed class NodeResiliencePipelineProvider : INodeResiliencePipelineProvider
{
    private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _pipelines = new();
    private readonly ResilienceOptions _options;

    public NodeResiliencePipelineProvider(IOptions<ResilienceOptions> options)
    {
        _options = options.Value;
    }

    public ResiliencePipeline<HttpResponseMessage> GetForNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node id is required.", nameof(nodeId));

        return _pipelines.GetOrAdd(nodeId, _ => BuildPipeline());
    }

    private ResiliencePipeline<HttpResponseMessage> BuildPipeline()
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay            = TimeSpan.FromSeconds(_options.RetryBaseDelaySeconds),
                MaxDelay         = TimeSpan.FromSeconds(_options.RetryMaxDelaySeconds),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = static args => ValueTask.FromResult(IsTransient(args.Outcome)),
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                MinimumThroughput = _options.CircuitBreakerMinimumThroughput,
                FailureRatio      = _options.CircuitBreakerFailureRatio,
                SamplingDuration  = TimeSpan.FromSeconds(_options.CircuitBreakerSamplingDurationSeconds),
                BreakDuration     = TimeSpan.FromSeconds(_options.CircuitBreakerBreakDurationSeconds),
                ShouldHandle      = static args => ValueTask.FromResult(IsTransient(args.Outcome)),
            });

        if (_options.TimeoutSeconds > 0)
            builder.AddTimeout(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        return builder.Build();
    }

    /// <summary>
    /// 4xx (except 408/429) is NOT transient: retrying wastes resources and masks
    /// the real client/config issue.
    /// </summary>
    private static bool IsTransient(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Result is { } response)
        {
            return response.StatusCode is System.Net.HttpStatusCode.RequestTimeout
                                       or System.Net.HttpStatusCode.TooManyRequests
                                       or >= System.Net.HttpStatusCode.InternalServerError;
        }

        return outcome.Exception is HttpRequestException
                                 or TimeoutRejectedException
                                 or TaskCanceledException
                                 or IOException
                                 or System.Net.Sockets.SocketException;
    }
}
