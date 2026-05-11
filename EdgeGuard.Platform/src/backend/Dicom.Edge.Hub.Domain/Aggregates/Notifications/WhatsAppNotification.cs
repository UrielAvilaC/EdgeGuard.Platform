using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Tracks a WhatsApp notification attempt for a study result delivery.
/// Immutable audit trail — one record per send attempt per recipient.
/// </summary>
public sealed class WhatsAppNotification : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public string? PatientId { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public string? NormalizedPhone { get; private set; }
    public string? TemplateId { get; private set; }
    public string? ContentSid { get; private set; }
    public string? StudyStatus { get; private set; }
    public WhatsAppNotificationStatus Status { get; private set; }
    public WhatsAppTriggerSource TriggeredBy { get; private set; }
    public string? ProviderName { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private WhatsAppNotification() { }

    public static WhatsAppNotification Create(
        string studyId,
        string phoneNumber,
        WhatsAppTriggerSource triggeredBy,
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

        return new WhatsAppNotification
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId.Trim(),
            PatientId = patientId?.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            NormalizedPhone = normalizedPhone?.Trim(),
            TemplateId = templateId?.Trim(),
            ContentSid = contentSid?.Trim(),
            StudyStatus = studyStatus?.Trim(),
            Status = WhatsAppNotificationStatus.Pending,
            TriggeredBy = triggeredBy,
            Attempts = 0
        };
    }

    public void MarkSent(string providerName, string? providerMessageId = null)
    {
        Status = WhatsAppNotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ProviderName = providerName;
        ProviderMessageId = providerMessageId?.Trim();
        Attempts++;
        LastError = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = WhatsAppNotificationStatus.Failed;
        Attempts++;
        LastError = error?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSkipped(string reason)
    {
        Status = WhatsAppNotificationStatus.Skipped;
        LastError = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
