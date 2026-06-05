using Dicom.Edge.Hub.Application.Messaging;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;

namespace Dicom.Edge.Hub.Infrastructure.Notifications;

/// <summary>
/// WhatsApp <see cref="INotificationChannelSender"/> — wraps the existing
/// <see cref="IMessagingProvider"/> (Twilio) so the transport is reused, not rewritten.
/// </summary>
public sealed class WhatsAppChannelSender(IMessagingProvider provider) : INotificationChannelSender
{
    public NotificationChannel Channel => NotificationChannel.WhatsApp;

    public async Task<ChannelSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(message.ContentSid))
            return new ChannelSendResult(false, null, "WhatsApp requires an approved ContentSid template.");

        var result = await provider.SendContentMessageAsync(
            message.To, message.ContentSid, message.Variables, ct);

        return new ChannelSendResult(result.Success, result.ProviderMessageId, result.Error);
    }
}
