namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Lifecycle status of a WhatsApp notification.
/// </summary>
public enum WhatsAppNotificationStatus
{
    Pending,
    Sent,
    Failed,
    Skipped
}

/// <summary>
/// Trigger source for the notification.
/// </summary>
public enum WhatsAppTriggerSource
{
    Automatic,
    Manual
}
