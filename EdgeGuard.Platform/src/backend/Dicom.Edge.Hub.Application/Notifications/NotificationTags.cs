namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>A predefined merge tag usable in notification templates.</summary>
public sealed record NotificationTag(string Tag, string Description, string Example);

/// <summary>
/// Catalog of predefined merge tags (<c>{{tag}}</c>) available to the Email template
/// editor and resolved at send time from the study/patient/delivery context.
/// </summary>
public static class NotificationTags
{
    public const string PatientName = "patientName";
    public const string PatientCode = "patientCode";
    public const string AccessionNumber = "accessionNumber";
    public const string StudyDescription = "studyDescription";
    public const string StudyDate = "studyDate";
    public const string Modality = "modality";
    public const string ReferringPhysician = "referringPhysician";
    public const string FacilityName = "facilityName";
    public const string ImageLink = "imageLink";
    public const string ReportLink = "reportLink";
    public const string QrCode = "qrCode";

    public static IReadOnlyList<NotificationTag> All =>
    [
        new(PatientName,       "Nombre del paciente",                "Juan Pérez"),
        new(PatientCode,       "Código/ID del paciente",             "PAT-00123"),
        new(AccessionNumber,   "Número de acceso (accession)",       "ACC-2026-0042"),
        new(StudyDescription,  "Descripción del estudio",            "TAC de tórax"),
        new(StudyDate,         "Fecha del estudio",                  "05/06/2026"),
        new(Modality,          "Modalidad",                          "CT"),
        new(ReferringPhysician,"Médico referente",                   "Dra. López"),
        new(FacilityName,      "Institución / sede",                 "Clínica Central"),
        new(ImageLink,         "Liga de imágenes",                   "https://viewer.example.com/s/abc"),
        new(ReportLink,        "Liga al reporte",                    "https://hub.example.com/report/abc"),
        new(QrCode,            "QR de la liga (cid en email)",       "<img src=\"cid:qr\">"),
    ];
}
