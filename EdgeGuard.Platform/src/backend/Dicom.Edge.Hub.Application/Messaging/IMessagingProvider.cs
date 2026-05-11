namespace Dicom.Edge.Hub.Application.Messaging;

/// <summary>
/// Provider-agnostic interface for sending WhatsApp content template messages.
/// Implement for each provider (Twilio, Meta, etc.).
/// </summary>
public interface IMessagingProvider
{
    string ProviderName { get; }

    Task<SendMessageResult> SendContentMessageAsync(
        string toPhoneNumber,
        string contentSid,
        Dictionary<int, string> variables,
        CancellationToken ct = default);
}
