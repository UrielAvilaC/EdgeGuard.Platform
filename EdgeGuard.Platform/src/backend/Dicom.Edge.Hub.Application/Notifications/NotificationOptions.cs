namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>Global notification behaviour. Bound from the <c>Notifications</c> section.</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>Master switch for automatic results delivery on study finalization.</summary>
    public bool AutoMode { get; set; }
}
