using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>Reads/writes notification operational settings (auto-mode + SMTP status/test).</summary>
public interface INotificationSettingsService
{
    Task<NotificationSettingsDto> GetAsync(CancellationToken ct = default);
    Task SetAutoModeAsync(bool autoMode, CancellationToken ct = default);
    /// <summary>Effective auto-mode: DB setting overrides appsettings. Used by the auto-delivery handler.</summary>
    Task<bool> ResolveAutoModeAsync(CancellationToken ct = default);
    Task<TestSmtpResponse> TestSmtpAsync(string toEmail, CancellationToken ct = default);
}

public sealed class NotificationSettingsService(
    ISystemSettingsService settings,
    IOptions<NotificationOptions> notificationOptions,
    IOptionsMonitor<SmtpOptions> smtpOptions,
    IEnumerable<INotificationChannelSender> channelSenders) : INotificationSettingsService
{
    public async Task<NotificationSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var smtp = smtpOptions.CurrentValue;
        return new NotificationSettingsDto
        {
            AutoMode = await ResolveAutoModeAsync(ct),
            SmtpEnabled = smtp.Enabled,
            SmtpHost = smtp.Host,
            SmtpFrom = smtp.From,
            SmtpPort = smtp.Port,
        };
    }

    public Task SetAutoModeAsync(bool autoMode, CancellationToken ct = default) =>
        settings.SetAsync(HubSettingKeys.Notifications.AutoMode, autoMode ? "true" : "false", ct);

    public async Task<bool> ResolveAutoModeAsync(CancellationToken ct = default)
    {
        var setting = await settings.GetAsync(HubSettingKeys.Notifications.AutoMode, ct);
        return setting is not null && bool.TryParse(setting.Value, out var value)
            ? value
            : notificationOptions.Value.AutoMode;
    }

    public async Task<TestSmtpResponse> TestSmtpAsync(string toEmail, CancellationToken ct = default)
    {
        var emailSender = channelSenders.FirstOrDefault(s => s.Channel == NotificationChannel.Email);
        if (emailSender is null)
            return new TestSmtpResponse { Success = false, Error = "Email channel not registered." };

        var result = await emailSender.SendAsync(new NotificationMessage
        {
            Channel = NotificationChannel.Email,
            To = toEmail,
            Subject = "EdgeGuard — prueba de SMTP",
            Body = "Este es un correo de prueba enviado desde EdgeGuard.",
            IsHtmlBody = false,
        }, ct);

        return new TestSmtpResponse { Success = result.Success, Error = result.Error };
    }
}
