using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// An Email notification template with a freeform HTML/plain-text body and merge tags
/// (<c>{{tag}}</c>) resolved at send time. Distinct from <see cref="WhatsAppTemplate"/>
/// (Twilio ContentSid) — the enterprise editor operates on these.
/// </summary>
public sealed class NotificationTemplate : AggregateRoot<string>
{
    public string Name { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; } = NotificationChannel.Email;
    /// <summary>Body format — <see cref="ReportFormat.Html"/> or <see cref="ReportFormat.PlainText"/>.</summary>
    public ReportFormat Format { get; private set; } = ReportFormat.Html;
    public string Subject { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(string name, ReportFormat format, string subject, string body)
    {
        Validate(name, subject, format);
        return new NotificationTemplate
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            Channel = NotificationChannel.Email,
            Format = NormalizeFormat(format),
            Subject = subject.Trim(),
            Body = body ?? string.Empty,
            IsActive = true
        };
    }

    public void Update(string name, ReportFormat format, string subject, string body)
    {
        Validate(name, subject, format);
        Name = name.Trim();
        Format = NormalizeFormat(format);
        Subject = subject.Trim();
        Body = body ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    private static ReportFormat NormalizeFormat(ReportFormat format) =>
        format == ReportFormat.PlainText ? ReportFormat.PlainText : ReportFormat.Html;

    private static void Validate(string name, string subject, ReportFormat format)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        if (format == ReportFormat.None)
            throw new ArgumentException("Template format must be Html or PlainText.", nameof(format));
    }
}
