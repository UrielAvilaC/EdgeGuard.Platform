using System.Text.Json;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Application.Outbox;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

/// <summary>
/// Durable notification outbox processor. Drains pending <c>Notification</c> records and
/// dispatches each to the matching <see cref="INotificationChannelSender"/>. Channels are
/// processed in independent lanes (a slow SMTP server does not block WhatsApp), and failures
/// are retried with exponential backoff via <c>NextAttemptAt</c>. The drain loop, interval
/// and backoff live in <see cref="OutboxDispatcherBase"/>.
/// </summary>
public sealed class NotificationOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    IQrCodeGenerator qrCodeGenerator,
    IOutboxNotifier notifier,
    ILogger<NotificationOutboxHostedService> logger)
    : OutboxDispatcherBase(scopeFactory, logger)
{
    private const int BatchSize = 100;
    private const int MaxAttempts = 5;

    protected override string WorkerName => "Notification outbox worker";
    protected override int IntervalSeconds => 30;

    protected override async Task DrainAsync(IServiceScope scope, CancellationToken ct)
    {
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
            await notifier.EntryChangedAsync(new OutboxEntryChange(
                OutboxTopicCatalog.Categories.Notification, n.Id, n.TopicId,
                n.Status.ToString(), n.Attempts, n.LastError), ct);
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
            Variables = DeserializeVariables(n.ContentVariables),
        },
    };

    /// <summary>Rehydrates the persisted JSON object (string position→value) into the
    /// position-keyed dictionary the WhatsApp sender / Twilio ContentVariables expects.</summary>
    private static Dictionary<int, string> DeserializeVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<int, string>();

        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return raw is null
            ? new Dictionary<int, string>()
            : raw.Where(kv => int.TryParse(kv.Key, out _))
                 .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }
}
