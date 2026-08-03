using System.Text.Encodings.Web;
using System.Text.Json;
using Dicom.Edge.Hub.Application.Messaging;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Twilio implementation of <see cref="IMessagingProvider"/>.
/// Reads encrypted provider config from system settings.
/// </summary>
public sealed class TwilioMessagingProvider(
    ISystemSettingRepository settingRepository,
    ISettingEncryptionService encryptionService,
    ILogger<TwilioMessagingProvider> logger) : IMessagingProvider
{
    public string ProviderName => MessagingProvider.Twilio.ToString();

    // Twilio ContentVariables is a JSON string. The default encoder escapes '&', '<', '>' as \uXXXX
    // (HTML-safe), which is valid JSON but noisy in logs and confusing for URL values. The relaxed
    // encoder keeps those characters literal; safe here since the JSON never lands in an HTML context.
    private static readonly JsonSerializerOptions ContentVariablesOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<SendMessageResult> SendContentMessageAsync(
        string toPhoneNumber,
        string contentSid,
        Dictionary<int, string> variables,
        CancellationToken ct = default)
    {
        try
        {
            var config = await LoadConfigAsync(ct);
            if (!config.IsComplete())
                return new SendMessageResult(false, null, "Twilio provider configuration is incomplete.");

            TwilioClient.Init(config.AccountSid, config.AuthToken);

            var contentVariables = variables.Count > 0
                ? JsonSerializer.Serialize(variables.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value), ContentVariablesOptions)
                : null;

            var options = new CreateMessageOptions(new PhoneNumber(ToWhatsAppAddress(toPhoneNumber)))
            {
                ContentSid = contentSid,
                ContentVariables = contentVariables,
            };

            // A Messaging Service and an explicit sender number are mutually exclusive.
            // Prefer the Messaging Service when configured; otherwise send from the number.
            var fromAddress = ToWhatsAppAddress(config.PhoneNumber);
            var sender = !string.IsNullOrWhiteSpace(config.MessagingServiceSid)
                ? $"MessagingServiceSid={config.MessagingServiceSid}"
                : $"From={fromAddress}";
            if (!string.IsNullOrWhiteSpace(config.MessagingServiceSid))
                options.MessagingServiceSid = config.MessagingServiceSid;
            else
                options.From = new PhoneNumber(fromAddress);

            logger.LogInformation(
                "Sending Twilio WhatsApp message to {To}: ContentSid={ContentSid} Sender={Sender} Variables={Variables}",
                toPhoneNumber, contentSid, sender, contentVariables ?? "{}");

            var message = await MessageResource.CreateAsync(options);

            logger.LogInformation(
                "Twilio message sent: SID={MessageSid} Status={Status} To={To} ContentSid={ContentSid} Variables={Variables}",
                message.Sid, message.Status, toPhoneNumber, contentSid, contentVariables ?? "{}");

            return new SendMessageResult(true, message.Sid, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twilio send failed to {Phone}", toPhoneNumber);
            return new SendMessageResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Builds a Twilio WhatsApp address (<c>whatsapp:+E164</c>), tolerating an already-prefixed
    /// value so the prefix is never duplicated (e.g. a config number stored as
    /// <c>whatsapp:+52...</c> would otherwise yield <c>whatsapp:whatsapp:+52...</c>, which Twilio rejects).
    /// </summary>
    private static string ToWhatsAppAddress(string? number)
    {
        var trimmed = (number ?? string.Empty).Trim();
        while (trimmed.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["whatsapp:".Length..].Trim();
        return $"whatsapp:{trimmed}";
    }

    private async Task<MessagingProviderConfig> LoadConfigAsync(CancellationToken ct)
    {
        var setting = await settingRepository.GetByKeyAsync(HubSettingKeys.WhatsApp.ProviderConfig, ct);
        if (setting is null)
            return new MessagingProviderConfig();

        var json = setting.IsEncrypted
            ? encryptionService.Decrypt(setting.Value)
            : setting.Value;

        return JsonSerializer.Deserialize<MessagingProviderConfig>(json) ?? new MessagingProviderConfig();
    }
}
