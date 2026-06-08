using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Maps a study status to a WhatsApp template for automatic delivery.
/// When a study transitions to the configured status and automatic delivery is enabled,
/// the associated template is sent to the patient's phone number.
/// One rule per study status (unique constraint).
/// </summary>
public sealed class WhatsAppAutoSendRule : Entity<string>
{
    public string StudyStatus { get; private set; } = default!;
    public string TemplateId { get; private set; } = default!;
    public bool IsEnabled { get; private set; }
    public string? Description { get; private set; }

    // Multichannel auto-send: channel + delivery toggles for this status.
    public string Channel { get; private set; } = "WhatsApp";   // "WhatsApp" | "Email"
    public bool AttachPdf { get; private set; }
    public bool IncludeQr { get; private set; }

    private WhatsAppAutoSendRule() { }

    public static WhatsAppAutoSendRule Create(
        string studyStatus,
        string templateId,
        string? description = null,
        string channel = "WhatsApp",
        bool attachPdf = false,
        bool includeQr = false)
    {
        if (string.IsNullOrWhiteSpace(studyStatus))
            throw new ArgumentException("Study status cannot be empty.", nameof(studyStatus));
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));

        return new WhatsAppAutoSendRule
        {
            Id = IdGenerator.NewId(),
            StudyStatus = studyStatus.Trim(),
            TemplateId = templateId.Trim(),
            IsEnabled = true,
            Description = description?.Trim(),
            Channel = string.IsNullOrWhiteSpace(channel) ? "WhatsApp" : channel.Trim(),
            AttachPdf = attachPdf,
            IncludeQr = includeQr
        };
    }

    /// <summary>Updates the delivery channel and PDF/QR toggles for this rule.</summary>
    public void SetDelivery(string channel, bool attachPdf, bool includeQr)
    {
        Channel = string.IsNullOrWhiteSpace(channel) ? "WhatsApp" : channel.Trim();
        AttachPdf = attachPdf;
        IncludeQr = includeQr;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignTemplate(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));
        TemplateId = templateId.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable() { IsEnabled = true; UpdatedAt = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAt = DateTime.UtcNow; }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
