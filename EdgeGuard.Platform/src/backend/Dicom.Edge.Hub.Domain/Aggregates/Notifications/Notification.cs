using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Unified notification record for study-result delivery over any channel
/// (WhatsApp or Email). Acts as the durable outbox row — one per recipient.
/// </summary>
public sealed class Notification : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public string? PatientId { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public string? NormalizedPhone { get; private set; }
    public string? TemplateId { get; private set; }
    public string? ContentSid { get; private set; }
    public string? StudyStatus { get; private set; }
    public NotificationStatus Status { get; private set; }
    public NotificationTriggerSource TriggeredBy { get; private set; }
    public string? ProviderName { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    // ── Unified outbox: channel + Email payload + retry scheduling ────────────
    public NotificationChannel Channel { get; private set; } = NotificationChannel.WhatsApp;

    /// <summary>FK to <c>outbox_topics</c>; derived from <see cref="Channel"/> at creation.</summary>
    public string TopicId { get; private set; } = OutboxTopicCatalog.NotificationWhatsApp;
    public string? ToEmail { get; private set; }
    public string? Subject { get; private set; }
    public string? RenderedBody { get; private set; }
    public bool IsHtmlBody { get; private set; }
    public string? AttachmentPath { get; private set; }
    /// <summary>Image-viewer link; when set, the email sender renders an inline QR (cid:qr).</summary>
    public string? ImageLink { get; private set; }
    /// <summary>Earliest time this record may be (re)sent — drives the retry backoff.</summary>
    public DateTime? NextAttemptAt { get; private set; }

    private Notification() { }

    public static Notification Create(
        string studyId,
        string phoneNumber,
        NotificationTriggerSource triggeredBy,
        string? studyStatus = null,
        string? patientId = null,
        string? normalizedPhone = null,
        string? templateId = null,
        string? contentSid = null)
    {
        if (string.IsNullOrWhiteSpace(studyId))
            throw new ArgumentException("Study ID cannot be empty.", nameof(studyId));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

        return new Notification
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId.Trim(),
            PatientId = patientId?.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            NormalizedPhone = normalizedPhone?.Trim(),
            TemplateId = templateId?.Trim(),
            ContentSid = contentSid?.Trim(),
            StudyStatus = studyStatus?.Trim(),
            Status = NotificationStatus.Pending,
            TriggeredBy = triggeredBy,
            Attempts = 0
        };
    }

    /// <summary>Creates a pending Email notification for the unified outbox.</summary>
    public static Notification CreateEmail(
        string studyId,
        string toEmail,
        string subject,
        string body,
        bool isHtmlBody,
        NotificationTriggerSource triggeredBy,
        string? studyStatus = null,
        string? patientId = null,
        string? templateId = null,
        string? attachmentPath = null,
        string? imageLink = null)
    {
        if (string.IsNullOrWhiteSpace(studyId))
            throw new ArgumentException("Study ID cannot be empty.", nameof(studyId));
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email cannot be empty.", nameof(toEmail));

        return new Notification
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId.Trim(),
            PatientId = patientId?.Trim(),
            PhoneNumber = string.Empty,
            Channel = NotificationChannel.Email,
            TopicId = OutboxTopicCatalog.NotificationEmail,
            ToEmail = toEmail.Trim(),
            Subject = subject,
            RenderedBody = body,
            IsHtmlBody = isHtmlBody,
            AttachmentPath = attachmentPath,
            ImageLink = imageLink,
            TemplateId = templateId?.Trim(),
            StudyStatus = studyStatus?.Trim(),
            Status = NotificationStatus.Pending,
            TriggeredBy = triggeredBy,
            Attempts = 0
        };
    }

    /// <summary>Returns the record to Pending with a future <see cref="NextAttemptAt"/> (backoff).</summary>
    public void ScheduleRetry(TimeSpan delay, string error)
    {
        Status = NotificationStatus.Pending;
        Attempts++;
        LastError = error?.Trim();
        NextAttemptAt = DateTime.UtcNow.Add(delay);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSent(string providerName, string? providerMessageId = null)
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ProviderName = providerName;
        ProviderMessageId = providerMessageId?.Trim();
        Attempts++;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.Failed;
        Attempts++;
        LastError = error?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSkipped(string reason)
    {
        Status = NotificationStatus.Skipped;
        LastError = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Manually returns the record to Pending for immediate re-dispatch.</summary>
    public void Requeue()
    {
        Status = NotificationStatus.Pending;
        NextAttemptAt = null;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
