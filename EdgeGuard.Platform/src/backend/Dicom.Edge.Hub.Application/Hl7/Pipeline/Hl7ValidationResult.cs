namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Result of HL7 message enterprise validation.
/// </summary>
public sealed class Hl7ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static Hl7ValidationResult Success(IReadOnlyList<string>? warnings = null) =>
        new() { IsValid = true, Warnings = warnings ?? [] };

    public static Hl7ValidationResult Failure(string error) =>
        new() { IsValid = false, ErrorMessage = error };
}
