using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// A Twilio Content Template definition. Stores the Twilio ContentSid
/// and the ordered list of variables that are resolved at send-time.
/// </summary>
public sealed class WhatsAppTemplate : AggregateRoot<string>
{
    private readonly List<WhatsAppTemplateVariable> _variables = [];

    public string Name { get; private set; } = default!;
    public string ContentSid { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyList<WhatsAppTemplateVariable> Variables => _variables.AsReadOnly();

    private WhatsAppTemplate() { }

    public static WhatsAppTemplate Create(
        string name,
        string contentSid,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(contentSid))
            throw new ArgumentException("ContentSid cannot be empty.", nameof(contentSid));

        return new WhatsAppTemplate
        {
            Id = IdGenerator.NewId(),
            Name = name.Trim(),
            ContentSid = contentSid.Trim(),
            Description = description?.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string contentSid, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(contentSid))
            throw new ArgumentException("ContentSid cannot be empty.", nameof(contentSid));

        Name = name.Trim();
        ContentSid = contentSid.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    /// <summary>
    /// Replaces the variable list with the given tags in sequential order (1-based).
    /// </summary>
    public void SetVariables(IEnumerable<string> tags)
    {
        _variables.Clear();
        var position = 1;
        foreach (var tag in tags)
            _variables.Add(WhatsAppTemplateVariable.Create(Id, position++, tag));
        UpdatedAt = DateTime.UtcNow;
    }

    public WhatsAppTemplateVariable AddVariable(string tag)
    {
        var nextPosition = _variables.Count + 1;
        var variable = WhatsAppTemplateVariable.Create(Id, nextPosition, tag);
        _variables.Add(variable);
        UpdatedAt = DateTime.UtcNow;
        return variable;
    }
}
