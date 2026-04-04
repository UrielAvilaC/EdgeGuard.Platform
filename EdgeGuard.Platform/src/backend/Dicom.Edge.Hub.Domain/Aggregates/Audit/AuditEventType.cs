namespace Dicom.Edge.Hub.Domain.Aggregates.Audit;

/// <summary>
/// Classification of hub audit events.
/// </summary>
public enum AuditEventType
{
    StudyReceived,
    StudyStatusChanged,
    StudySentToPacs,
    StudyFailed,
    NodeRegistered,
    NodeHeartbeat,
    NodeStatusChanged,
    HealthCheckReceived,
    SettingChanged,
    WhatsAppNotificationSent,
    WhatsAppNotificationFailed,
    DomainEvent,
    UserAction,
    SystemEvent,
    NodeConfigChanged,
    NodeConfigPushed,
    NodeConfigAcknowledged,
    NodeConfigPushFailed
}
