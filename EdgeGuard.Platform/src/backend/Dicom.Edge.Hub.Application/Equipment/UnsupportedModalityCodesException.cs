namespace Dicom.Edge.Hub.Application.Equipment;

/// <summary>
/// Thrown when an equipment create/update request references modality codes that are
/// unknown to the catalog or not assignable (not supported, or inactive).
/// The API layer translates this into a 400 Bad Request listing the offending codes.
/// </summary>
public sealed class UnsupportedModalityCodesException(IReadOnlyList<string> codes)
    : Exception($"The following modality codes are unknown or not assignable: {string.Join(", ", codes)}")
{
    public IReadOnlyList<string> Codes { get; } = codes;
}
