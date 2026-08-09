using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Shared skeleton for durable outbox dispatchers. Owns the supervised drain loop
/// (fixed interval, fresh DI scope per cycle, swallow-and-log on failure) and the
/// exponential-backoff helper. Subclasses implement <see cref="DrainAsync"/> with their
/// store-specific batch / send / persist logic.
/// </summary>
public abstract class OutboxDispatcherBase(
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    /// <summary>Human-readable worker name used in start/stop/error logs.</summary>
    protected abstract string WorkerName { get; }

    /// <summary>Seconds to wait between drain cycles.</summary>
    protected abstract int IntervalSeconds { get; }

    /// <summary>Drains one batch within the provided DI scope. Invoked once per interval.</summary>
    protected abstract Task DrainAsync(IServiceScope scope, CancellationToken ct);

    /// <summary>Exponential backoff capped at 5 minutes: 2^(attempts+1) seconds.</summary>
    protected static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, attempts + 1)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Worker} started", WorkerName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await DrainAsync(scope, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "{Worker} drain failed", WorkerName);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("{Worker} stopped", WorkerName);
    }
}
