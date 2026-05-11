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
                ? JsonSerializer.Serialize(variables.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value))
                : null;

            var message = await MessageResource.CreateAsync(
                to: new PhoneNumber($"whatsapp:{toPhoneNumber}"),
                from: new PhoneNumber($"whatsapp:{config.PhoneNumber}"),
                contentSid: contentSid,
                contentVariables: contentVariables,
                messagingServiceSid: config.MessagingServiceSid);

            logger.LogInformation("Twilio message sent: SID={MessageSid} To={To}",
                message.Sid, toPhoneNumber);

            return new SendMessageResult(true, message.Sid, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Twilio send failed to {Phone}", toPhoneNumber);
            return new SendMessageResult(false, null, ex.Message);
        }
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
