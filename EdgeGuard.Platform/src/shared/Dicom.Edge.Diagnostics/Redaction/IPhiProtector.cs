namespace Dicom.Edge.Diagnostics.Redaction;

/// <summary>
/// Contract for PHI (Protected Health Information) redaction in log events.
/// Implementations must be thread-safe as they are called from the Serilog pipeline.
/// </summary>
public interface IPhiProtector
{
    /// <summary>Redacts PHI from a string value.</summary>
    string Redact(string? value);

    /// <summary>Determines whether a given property name is classified as PHI.</summary>
    bool IsPhiProperty(string propertyName);
}
