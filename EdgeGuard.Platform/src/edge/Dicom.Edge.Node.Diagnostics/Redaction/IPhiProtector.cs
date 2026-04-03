namespace Dicom.Edge.Node.Diagnostics.Redaction;

/// <summary>
/// Contract for PHI (Protected Health Information) redaction in log events.
/// Implementations must be thread-safe as they are called from Serilog pipeline.
/// </summary>
public interface IPhiProtector
{
    /// <summary>
    /// Redacts PHI from a string value.
    /// </summary>
    /// <param name="value">The raw value that may contain PHI.</param>
    /// <returns>The redacted value.</returns>
    string Redact(string? value);

    /// <summary>
    /// Determines whether a given property name is classified as PHI.
    /// </summary>
    /// <param name="propertyName">The log property name to evaluate.</param>
    /// <returns><c>true</c> if the property should be redacted.</returns>
    bool IsPhiProperty(string propertyName);
}
