namespace Dicom.Edge.Contracts.Notifications;

public sealed record NotificationSettingsDto
{
    public bool AutoMode { get; init; }
    public bool SmtpEnabled { get; init; }
    public string SmtpHost { get; init; } = string.Empty;
    public string SmtpFrom { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
}

public sealed record SetAutoModeRequest
{
    public bool AutoMode { get; init; }
}

public sealed record TestSmtpRequest
{
    public required string ToEmail { get; init; }
}

public sealed record TestSmtpResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}
