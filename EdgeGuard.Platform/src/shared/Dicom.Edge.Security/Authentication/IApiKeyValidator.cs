namespace Dicom.Edge.Security.Authentication;

/// <summary>
/// Abstraction for validating API keys used in M2M (node-to-hub) authentication.
/// Implemented in the Infrastructure/Persistence layer where Node repository is available.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates the provided API key and returns the authenticated node identity, or null if invalid.
    /// </summary>
    Task<ApiKeyValidationResult?> ValidateAsync(string apiKey, CancellationToken ct = default);
}

/// <summary>
/// Result of a successful API key validation.
/// </summary>
public sealed record ApiKeyValidationResult(string NodeId, string NodeName, bool IsEnabled);
