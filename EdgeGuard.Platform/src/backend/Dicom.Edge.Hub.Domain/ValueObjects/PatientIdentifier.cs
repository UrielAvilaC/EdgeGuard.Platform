namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// DICOM Patient Identifier value object.
/// </summary>
public sealed record PatientIdentifier
{
    public string Value { get; }

    private PatientIdentifier(string value) => Value = value;

    public static PatientIdentifier Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Patient ID cannot be empty.", nameof(value));

        return new PatientIdentifier(value.Trim());
    }

    public override string ToString() => Value;

    public static implicit operator string(PatientIdentifier id) => id.Value;
}
