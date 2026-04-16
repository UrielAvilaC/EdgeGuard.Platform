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
    WhatsAppNotificationSkipped,
    WhatsAppTemplateCreated,
    WhatsAppTemplateUpdated,
    WhatsAppTemplateDeleted,
    WhatsAppAutoSendRuleChanged,
    WhatsAppManualSendRequested,
    PatientContactUpdatedFromHl7,
    DomainEvent,
    UserAction,
    SystemEvent,
    NodeConfigChanged,
    NodeConfigPushed,
    NodeConfigAcknowledged,
    NodeConfigPushFailed
}
