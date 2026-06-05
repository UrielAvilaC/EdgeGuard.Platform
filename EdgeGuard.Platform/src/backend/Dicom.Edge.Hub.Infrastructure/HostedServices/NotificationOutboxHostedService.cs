using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Unified, durable notification outbox processor. Drains pending <c>Notification</c>
/// records and dispatches each to the matching <see cref="INotificationChannelSender"/>.
/// Channels are processed in independent lanes (a slow SMTP server does not block
/// WhatsApp), and failures are retried with exponential backoff via <c>NextAttemptAt</c>.
/// Replaces the WhatsApp-only hosted service.
/// </summary>
public sealed class NotificationOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    IQrCodeGenerator qrCodeGenerator,
    ILogger<NotificationOutboxHostedService> logger) : BackgroundService
{
    private const int IntervalSeconds = 30;
    private const int BatchSize = 100;
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification outbox worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Notification outbox drain failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Notification outbox worker stopped");
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var senders = scope.ServiceProvider.GetServices<INotificationChannelSender>()
            .ToDictionary(s => s.Channel);

        var due = await repo.GetDuePendingAsync(BatchSize, ct);
        if (due.Count == 0) return;

        // Per-channel lanes: isolate failures/latency between channels.
        var lanes = due
            .GroupBy(n => n.Channel)
            .Select(group => ProcessChannelAsync(group.Key, [.. group], senders, repo, ct));
        await Task.WhenAll(lanes);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ProcessChannelAsync(
        NotificationChannel channel,
        List<Notification> items,
        IReadOnlyDictionary<NotificationChannel, INotificationChannelSender> senders,
        INotificationRepository repo,
        CancellationToken ct)
    {
        if (!senders.TryGetValue(channel, out var sender))
        {
            foreach (var n in items)
            {
                n.MarkFailed($"No sender registered for channel {channel}");
                await repo.UpdateAsync(n, ct);
            }
            return;
        }

        foreach (var n in items)
        {
            var result = await sender.SendAsync(ToMessage(n), ct);

            if (result.Success)
                n.MarkSent(channel.ToString(), result.ProviderMessageId);
            else if (n.Attempts + 1 >= MaxAttempts)
                n.MarkFailed(result.Error ?? "Send failed");
            else
                n.ScheduleRetry(Backoff(n.Attempts), result.Error ?? "Send failed");

            await repo.UpdateAsync(n, ct);
        }
    }

    private NotificationMessage ToMessage(Notification n) => n.Channel switch
    {
        NotificationChannel.Email => new NotificationMessage
        {
            Channel = NotificationChannel.Email,
            To = n.ToEmail ?? string.Empty,
            Subject = n.Subject,
            Body = n.RenderedBody,
            IsHtmlBody = n.IsHtmlBody,
            AttachmentPath = n.AttachmentPath,
            // Render the inline QR (cid:qr) when an image link is present.
            QrPng = string.IsNullOrEmpty(n.ImageLink) ? null : qrCodeGenerator.GeneratePng(n.ImageLink),
        },
        _ => new NotificationMessage
        {
            Channel = NotificationChannel.WhatsApp,
            To = n.NormalizedPhone ?? n.PhoneNumber,
            ContentSid = n.ContentSid,
        },
    };

    private static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, attempts + 1)));
}
