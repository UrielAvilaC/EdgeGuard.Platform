using System.Text.Json;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>
/// Default dispatcher: turns a <see cref="DeliveryRequest"/> into pending
/// <see cref="Notification"/> rows (the unified outbox) and saves them.
/// </summary>
public sealed class NotificationDispatcher(
    INotificationRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task<DeliveryResult> DeliverAsync(DeliveryRequest request, CancellationToken ct = default)
    {
        var count = 0;

        foreach (var target in request.Targets)
        {
            var notification = target.Channel switch
            {
                NotificationChannel.Email => Notification.CreateEmail(
                    request.StudyId,
                    target.To,
                    target.Subject ?? "Resultados de estudio",
                    target.Body ?? string.Empty,
                    target.IsHtmlBody,
                    request.TriggeredBy,
                    request.StudyStatus,
                    request.PatientId,
                    target.TemplateId,
                    target.AttachmentPath,
                    target.ImageLink),

                _ => Notification.Create(
                    request.StudyId,
                    target.To,
                    request.TriggeredBy,
                    request.StudyStatus,
                    request.PatientId,
                    target.NormalizedPhone ?? target.To,
                    target.TemplateId,
                    target.ContentSid,
                    SerializeVariables(target.Variables)),
            };

            await repository.AddAsync(notification, ct);
            count++;
        }

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enqueued {Count} notification(s) for study {StudyId} across {Channels} channel(s)",
            count, request.StudyId, request.Targets.Select(t => t.Channel).Distinct().Count());

        return new DeliveryResult(count);
    }

    /// <summary>Serializes resolved WhatsApp variables to a JSON object of position→value
    /// (string keys, as Twilio's ContentVariables expects). Returns null when there are none.</summary>
    private static string? SerializeVariables(IReadOnlyDictionary<int, string>? variables) =>
        variables is null or { Count: 0 }
            ? null
            : JsonSerializer.Serialize(variables.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
}
