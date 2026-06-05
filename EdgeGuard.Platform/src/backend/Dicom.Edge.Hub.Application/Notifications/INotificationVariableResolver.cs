using System.Text.RegularExpressions;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>
/// Renders notification template bodies/subjects by replacing <c>{{tag}}</c> merge tags
/// with values from the study/patient/delivery context (or sample values for previews).
/// </summary>
public interface INotificationVariableResolver
{
    string Render(string template, IReadOnlyDictionary<string, string> values);
    IReadOnlyDictionary<string, string> SampleValues();
    IReadOnlyDictionary<string, string> BuildValues(Study study, Patient? patient, string? imageLink, string? reportLink);
}

public sealed partial class NotificationVariableResolver : INotificationVariableResolver
{
    [GeneratedRegex(@"\{\{\s*(\w+)\s*\}\}")]
    private static partial Regex TagPattern();

    public string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(template)) return template ?? string.Empty;

        return TagPattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    public IReadOnlyDictionary<string, string> SampleValues() =>
        NotificationTags.All.ToDictionary(t => t.Tag, t => t.Example);

    public IReadOnlyDictionary<string, string> BuildValues(
        Study study, Patient? patient, string? imageLink, string? reportLink) =>
        new Dictionary<string, string>
        {
            [NotificationTags.PatientName]       = study.PatientName ?? patient?.PatientName ?? "",
            [NotificationTags.PatientCode]       = patient?.PatientDicomId.Value ?? study.PatientId ?? "",
            [NotificationTags.AccessionNumber]   = study.AccessionNumber ?? "",
            [NotificationTags.StudyDescription]  = study.StudyDescription ?? "",
            [NotificationTags.StudyDate]         = study.StudyDate?.ToString("dd/MM/yyyy") ?? "",
            [NotificationTags.Modality]          = study.Series.FirstOrDefault()?.Modality ?? "",
            [NotificationTags.ReferringPhysician]= study.ReferringPhysician ?? "",
            [NotificationTags.FacilityName]      = "",
            [NotificationTags.ImageLink]         = imageLink ?? "",
            [NotificationTags.ReportLink]        = reportLink ?? "",
            [NotificationTags.QrCode]            = imageLink is not null ? "<img src=\"cid:qr\" alt=\"QR\">" : "",
        };
}
