using Dicom.Edge.Hub.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class StudyCleanupEvaluationHostedService : BackgroundService
{
    private readonly IStudyCleanupService _studyCleanupService;
    private readonly ILogger<StudyCleanupEvaluationHostedService> _logger;
    private readonly HubBackgroundJobsOptions _options;

    public StudyCleanupEvaluationHostedService(
        IStudyCleanupService studyCleanupService,
        IOptions<HubBackgroundJobsOptions> options,
        ILogger<StudyCleanupEvaluationHostedService> logger)
    {
        _studyCleanupService = studyCleanupService;
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
                var eligibleStudies = await _studyCleanupService.GetStudiesEligibleForCleanupAsync(stoppingToken);

                if (eligibleStudies.Count > 0)
                {
                    _logger.LogInformation("Study cleanup evaluation found {Count} eligible studies", eligibleStudies.Count);
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
