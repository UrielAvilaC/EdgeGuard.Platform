namespace Dicom.Edge.Hub.Domain.Aggregates.Audit;

/// <summary>
/// Severity classification of hub audit entries.
/// </summary>
public enum AuditSeverity
{
    Information,
    Warning,
    Error,
    Critical
}
