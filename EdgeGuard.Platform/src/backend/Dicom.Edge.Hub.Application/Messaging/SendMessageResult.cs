namespace Dicom.Edge.Hub.Application.Messaging;

/// <summary>
/// Result of a messaging provider send operation.
/// </summary>
public sealed record SendMessageResult(
    bool Success,
    string? ProviderMessageId,
    string? Error);
