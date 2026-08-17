using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Application.Reports;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Dicom.Edge.Hub.Infrastructure.Notifications;

/// <summary>
/// Email <see cref="INotificationChannelSender"/> over SMTP (MailKit). Renders the
/// message body (HTML/text), attaches the report PDF (from <see cref="IReportStorage"/>)
/// when requested, and embeds the QR as an inline (cid:qr) resource.
/// </summary>
public sealed class EmailChannelSender(
    IOptionsMonitor<SmtpOptions> options,
    IReportStorage reportStorage,
    ILogger<EmailChannelSender> logger) : INotificationChannelSender
{
    // CurrentValue so UI edits to the DB-backed Smtp:* settings apply without a restart
    // (after IConfigurationRoot.Reload() runs on save).
    private SmtpOptions Opts => options.CurrentValue;

    public NotificationChannel Channel => NotificationChannel.Email;

    public async Task<ChannelSendResult> SendAsync(NotificationMessage message, CancellationToken ct = default)
    {
        if (!Opts.Enabled)
            return new ChannelSendResult(false, null, "SMTP is disabled (Smtp:Enabled=false).");

        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(Opts.FromName, Opts.From));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject ?? "Resultados de estudio";

            var builder = new BodyBuilder();
            if (message.IsHtmlBody) builder.HtmlBody = message.Body;
            else builder.TextBody = message.Body;

            if (message.QrPng is { Length: > 0 })
            {
                var image = builder.LinkedResources.Add("qr.png", message.QrPng,
                    new ContentType("image", "png"));
                image.ContentId = "qr";
            }

            if (!string.IsNullOrEmpty(message.AttachmentPath))
            {
                var stream = await reportStorage.OpenPdfAsync(message.AttachmentPath, ct);
                if (stream is not null)
                {
                    await using (stream)
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms, ct);
                        builder.Attachments.Add("report.pdf", ms.ToArray(),
                            new ContentType("application", "pdf"));
                    }
                }
            }

            mime.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            var socketOptions = Opts.UseTls ? SecureSocketOptions.SslOnConnect: SecureSocketOptions.Auto;
            await client.ConnectAsync(Opts.Host, Opts.Port, socketOptions, ct);
            if (!string.IsNullOrEmpty(Opts.User))
                await client.AuthenticateAsync(Opts.User, Opts.Password, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            return new ChannelSendResult(true, mime.MessageId, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email send to {To} failed", message.To);
            return new ChannelSendResult(false, null, ex.Message);
        }
    }
}
