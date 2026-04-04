using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Tracks a WhatsApp notification attempt for a study result delivery.
/// </summary>
public sealed class WhatsAppNotification : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public string? PatientId { get; private set; }
    public string PhoneNumber { get; private set; } = default!;
    public string? PacsViewerLink { get; private set; }
    public string? MessageTemplate { get; private set; }
    public WhatsAppNotificationStatus Status { get; private set; }
    public WhatsAppTriggerSource TriggeredBy { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private WhatsAppNotification() { }

    public static WhatsAppNotification Create(
        string studyId,
        string phoneNumber,
        WhatsAppTriggerSource triggeredBy,
        string? patientId = null,
        string? pacsViewerLink = null,
        string? messageTemplate = null)
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
            PacsViewerLink = pacsViewerLink?.Trim(),
            MessageTemplate = messageTemplate?.Trim(),
            Status = WhatsAppNotificationStatus.Pending,
            TriggeredBy = triggeredBy,
            Attempts = 0
        };
    }

    public void MarkSent()
    {
        Status = WhatsAppNotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
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
