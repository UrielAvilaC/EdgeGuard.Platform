using System.Text.RegularExpressions;

namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// DICOM Unique Identifier (UID). Validates format per DICOM standard (digits and dots, max 64 chars).
/// </summary>
public sealed partial record DicomUid
{
    public string Value { get; }

    private DicomUid(string value) => Value = value;

    public static DicomUid Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("DICOM UID cannot be empty.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
            throw new ArgumentException("DICOM UID cannot exceed 64 characters.", nameof(value));

        if (!UidPattern().IsMatch(trimmed))
            throw new ArgumentException("DICOM UID must contain only digits and dots.", nameof(value));

        return new DicomUid(trimmed);
    }

    public override string ToString() => Value;

    public static implicit operator string(DicomUid uid) => uid.Value;

    [GeneratedRegex(@"^[0-9][0-9.]*$")]
    private static partial Regex UidPattern();
}
