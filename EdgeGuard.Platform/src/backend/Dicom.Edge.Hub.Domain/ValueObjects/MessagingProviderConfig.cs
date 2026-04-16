namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// Represents the provider-specific credentials stored as encrypted JSON in system_settings.
/// Deserialized from the <c>whatsapp.provider_config</c> setting.
/// </summary>
public sealed record MessagingProviderConfig
{
    public string? AccountSid { get; init; }
    public string? AuthToken { get; init; }
    public string? PhoneNumber { get; init; }
    public string? MessagingServiceSid { get; init; }

    /// <summary>
    /// Returns <c>true</c> if all required fields are populated.
    /// </summary>
    public bool IsComplete() =>
        !string.IsNullOrWhiteSpace(AccountSid) &&
        !string.IsNullOrWhiteSpace(AuthToken) &&
        !string.IsNullOrWhiteSpace(PhoneNumber) &&
        !string.IsNullOrWhiteSpace(MessagingServiceSid);
}
