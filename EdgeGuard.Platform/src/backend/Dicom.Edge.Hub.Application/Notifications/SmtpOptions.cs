namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>SMTP configuration for the Email delivery channel. Bound from <c>Smtp</c>.</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@edgeguard.local";
    public string FromName { get; set; } = "EdgeGuard";
    public bool UseTls { get; set; } = true;
}
