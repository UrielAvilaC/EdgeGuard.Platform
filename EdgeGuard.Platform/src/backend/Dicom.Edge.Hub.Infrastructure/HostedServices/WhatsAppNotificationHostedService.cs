using Dicom.Edge.Hub.Application.WhatsApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Background service that periodically processes pending WhatsApp notifications.
/// </summary>
public sealed class WhatsAppNotificationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<WhatsAppNotificationHostedService> logger) : BackgroundService
{
    private const int DefaultIntervalSeconds = 60;
    private const int DefaultBatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WhatsApp notification background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();
                await service.ProcessPendingAsync(DefaultBatchSize, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing pending WhatsApp notifications");
            }

            await Task.Delay(TimeSpan.FromSeconds(DefaultIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("WhatsApp notification background service stopped");
    }
}
