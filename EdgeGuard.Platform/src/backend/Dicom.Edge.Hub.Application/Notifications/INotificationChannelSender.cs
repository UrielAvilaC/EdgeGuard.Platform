using Dicom.Edge.Hub.Domain.Aggregates.Notifications;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>
/// A fully-resolved message ready to be sent over a single channel. For WhatsApp it
/// carries a Twilio <see cref="ContentSid"/> + ordered <see cref="Variables"/>; for
/// Email it carries the rendered <see cref="Subject"/>/<see cref="Body"/> and optional
/// PDF attachment / inline QR.
/// </summary>
public sealed record NotificationMessage
{
    public required NotificationChannel Channel { get; init; }
    public required string To { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public bool IsHtmlBody { get; init; }
    public string? ContentSid { get; init; }
    public Dictionary<int, string> Variables { get; init; } = new();
    public string? AttachmentPath { get; init; }
    public byte[]? QrPng { get; init; }
}

/// <summary>Outcome of a single channel send.</summary>
public sealed record ChannelSendResult(bool Success, string? ProviderMessageId, string? Error);

/// <summary>
/// Channel-agnostic sender. Implemented per channel (WhatsApp, Email). The dispatcher
/// selects the implementation whose <see cref="Channel"/> matches the message — Email
/// does NOT inherit from the WhatsApp abstraction; both are siblings.
/// </summary>
public interface INotificationChannelSender
{
    NotificationChannel Channel { get; }
    Task<ChannelSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default);
}
