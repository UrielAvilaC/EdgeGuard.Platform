using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class StudyCleanupEvaluationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StudyCleanupEvaluationHostedService> _logger;
    private readonly HubBackgroundJobsOptions _options;

    public StudyCleanupEvaluationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<HubBackgroundJobsOptions> options,
        ILogger<StudyCleanupEvaluationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableStudyCleanupEvaluator)
        {
            _logger.LogInformation("Study cleanup evaluator worker is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(30, _options.StudyCleanupEvaluationIntervalSeconds));

        _logger.LogInformation("Study cleanup evaluator worker started with interval {IntervalSeconds}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IStudyCleanupService>();

                var deleted = await cleanupService.ExecuteCleanupAsync(
                    _options.DataRetention.BatchSize, stoppingToken);

                if (deleted > 0)
                {
                    _logger.LogInformation("Study cleanup cycle completed: {DeletedCount} studies soft-deleted", deleted);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Study cleanup evaluator iteration failed");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }

        _logger.LogInformation("Study cleanup evaluator worker stopped");
    }
}
