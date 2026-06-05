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
                    target.ContentSid),
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
}
