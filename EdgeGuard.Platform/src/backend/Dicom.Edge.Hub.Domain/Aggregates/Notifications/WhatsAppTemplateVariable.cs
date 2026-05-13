using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// A positional variable in a WhatsApp Content Template.
/// Maps a 1-based position to a predefined tag resolved at send-time.
/// </summary>
public sealed class WhatsAppTemplateVariable : Entity<string>
{
    public string TemplateId { get; private set; } = default!;
    public int Position { get; private set; }
    public string Tag { get; private set; } = default!;

    private WhatsAppTemplateVariable() { }

    public static WhatsAppTemplateVariable Create(string templateId, int position, string tag)
    {
        if (string.IsNullOrWhiteSpace(templateId))
            throw new ArgumentException("Template ID cannot be empty.", nameof(templateId));
        if (position < 1)
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be >= 1.");
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));

        return new WhatsAppTemplateVariable
        {
            Id = IdGenerator.NewId(),
            TemplateId = templateId.Trim(),
            Position = position,
            Tag = tag.Trim()
        };
    }
}
