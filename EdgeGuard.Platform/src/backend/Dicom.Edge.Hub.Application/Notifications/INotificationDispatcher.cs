using Dicom.Edge.Hub.Domain.Aggregates.Notifications;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>A single resolved recipient + channel payload for a delivery.</summary>
public sealed record DeliveryTarget
{
    public required NotificationChannel Channel { get; init; }
    public required string To { get; init; }

    // WhatsApp
    public string? ContentSid { get; init; }
    public string? TemplateId { get; init; }
    public string? NormalizedPhone { get; init; }
    /// <summary>Resolved positional Content template variables (position → value).</summary>
    public IReadOnlyDictionary<int, string>? Variables { get; init; }

    // Email
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public bool IsHtmlBody { get; init; }
    public string? AttachmentPath { get; init; }
    public string? ImageLink { get; init; }
}

/// <summary>Request to deliver a study's results over one or more channels.</summary>
public sealed record DeliveryRequest
{
    public required string StudyId { get; init; }
    public string? PatientId { get; init; }
    public string? StudyStatus { get; init; }
    public NotificationTriggerSource TriggeredBy { get; init; } = NotificationTriggerSource.Manual;
    public required IReadOnlyList<DeliveryTarget> Targets { get; init; }
}

public sealed record DeliveryResult(int Enqueued);

/// <summary>
/// Entry point for results delivery. Resolves each target into a pending
/// <c>Notification</c> row (the durable outbox) and returns immediately; the
/// <c>NotificationOutboxHostedService</c> performs the actual sends per channel.
/// </summary>
public interface INotificationDispatcher
{
    Task<DeliveryResult> DeliverAsync(DeliveryRequest request, CancellationToken ct = default);
}
