using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class NodeHealthEvaluationHostedService : BackgroundService
{
    private readonly INodeHealthEvaluator _nodeHealthEvaluator;
    private readonly ILogger<NodeHealthEvaluationHostedService> _logger;
    private readonly HubBackgroundJobsOptions _options;

    public NodeHealthEvaluationHostedService(
        INodeHealthEvaluator nodeHealthEvaluator,
        IOptions<HubBackgroundJobsOptions> options,
        ILogger<NodeHealthEvaluationHostedService> logger)
    {
        _nodeHealthEvaluator = nodeHealthEvaluator;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableNodeHealthEvaluator)
        {
            _logger.LogInformation("Node health evaluator worker is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.NodeHealthEvaluationIntervalSeconds));

        _logger.LogInformation("Node health evaluator worker started with interval {IntervalSeconds}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _nodeHealthEvaluator.EvaluateAllNodesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Node health evaluator iteration failed");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }

        _logger.LogInformation("Node health evaluator worker stopped");
    }
}
