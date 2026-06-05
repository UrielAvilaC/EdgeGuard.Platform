namespace Dicom.Edge.Contracts.Notifications;

public sealed record NotificationTemplateDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Format { get; init; }   // "Html" | "PlainText"
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record CreateNotificationTemplateRequest
{
    public required string Name { get; init; }
    public string Format { get; init; } = "Html";
    public required string Subject { get; init; }
    public string Body { get; init; } = string.Empty;
}

public sealed record UpdateNotificationTemplateRequest
{
    public required string Name { get; init; }
    public string Format { get; init; } = "Html";
    public required string Subject { get; init; }
    public string Body { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed record NotificationTagDto
{
    public required string Tag { get; init; }
    public required string Description { get; init; }
    public required string Example { get; init; }
}

public sealed record PreviewTemplateRequest
{
    public string Format { get; init; } = "Html";
    public required string Subject { get; init; }
    public string Body { get; init; } = string.Empty;
}

public sealed record PreviewTemplateResponse
{
    public required string Subject { get; init; }
    public required string Body { get; init; }
}
