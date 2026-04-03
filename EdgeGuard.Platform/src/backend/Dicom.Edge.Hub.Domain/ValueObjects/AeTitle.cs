namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// DICOM Application Entity Title. Max 16 characters, trimmed, uppercase.
/// </summary>
public sealed record AeTitle
{
    public string Value { get; }

    private AeTitle(string value) => Value = value;

    public static AeTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AE Title cannot be empty.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length > 16)
            throw new ArgumentException("AE Title cannot exceed 16 characters.", nameof(value));

        return new AeTitle(trimmed.ToUpperInvariant());
    }

    public override string ToString() => Value;

    public static implicit operator string(AeTitle aeTitle) => aeTitle.Value;
}
