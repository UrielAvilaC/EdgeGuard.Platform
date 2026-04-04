using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Audit;

/// <summary>
/// Immutable audit log entry for the Hub. Tracks all significant system and user actions.
/// </summary>
public sealed class HubAuditLog : Entity<string>
{
    public AuditEventType EventType { get; private set; }
    public string Action { get; private set; } = default!;
    public AuditSeverity Severity { get; private set; }

    public string? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }

    public string? EntityId { get; private set; }
    public string? EntityType { get; private set; }

    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? Details { get; private set; }

    private HubAuditLog() { }

    public static HubAuditLog Create(
        AuditEventType eventType,
        string action,
        AuditSeverity severity = AuditSeverity.Information,
        string? userId = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null,
        string? entityId = null,
        string? entityType = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Audit action cannot be empty.", nameof(action));

        return new HubAuditLog
        {
            Id = IdGenerator.NewId(),
            EventType = eventType,
            Action = action.Trim(),
            Severity = severity,
            UserId = userId?.Trim(),
            UserName = userName?.Trim(),
            IpAddress = ipAddress?.Trim(),
            CorrelationId = correlationId?.Trim(),
            EntityId = entityId?.Trim(),
            EntityType = entityType?.Trim(),
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage?.Trim(),
            Details = details?.Trim()
        };
    }
}
