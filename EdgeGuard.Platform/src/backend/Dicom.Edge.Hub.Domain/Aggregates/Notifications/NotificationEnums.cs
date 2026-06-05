namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Lifecycle status of a WhatsApp notification.
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Skipped
}

/// <summary>
/// Trigger source for the notification.
/// </summary>
public enum NotificationTriggerSource
{
    Automatic,
    Manual
}

/// <summary>
/// Supported messaging providers for WhatsApp delivery.
/// </summary>
public enum MessagingProvider
{
    Twilio,
    Meta
}

/// <summary>Delivery channel of a unified notification record.</summary>
public enum NotificationChannel
{
    WhatsApp,
    Email
}

/// <summary>
/// Well-known variable tags that can be used in WhatsApp content templates.
/// Each tag is resolved at send-time from the study/patient context.
/// </summary>
public static class WhatsAppTemplateTags
{
    public const string AccessionNumber = "accessionNumber";
    public const string PatientCode = "patientCode";
    public const string PatientName = "patientName";
    public const string StudyDate = "studyDate";
    public const string AppointmentDate = "appointmentDate";
    public const string StudyDescription = "studyDescription";
    public const string Modality = "modality";
    public const string ReferringPhysician = "referringPhysician";
    public const string InstitutionName = "institutionName";
    public const string PacsViewerLink = "pacsViewerLink";
    public const string ImagesUrl = "imagesUrl";

    public static IReadOnlyList<string> All =>
    [
        AccessionNumber, PatientCode, PatientName, StudyDate,
        AppointmentDate, StudyDescription, Modality,
        ReferringPhysician, InstitutionName, PacsViewerLink, ImagesUrl
    ];
}
