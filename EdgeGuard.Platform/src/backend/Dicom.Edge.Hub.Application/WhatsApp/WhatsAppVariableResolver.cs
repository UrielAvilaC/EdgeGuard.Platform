using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Application.WhatsApp;

/// <summary>
/// Resolves WhatsApp template variable tags to actual values from study/patient context.
/// </summary>
public static class WhatsAppVariableResolver
{
    public static Dictionary<int, string> Resolve(
        Study study,
        Patient? patient,
        IReadOnlyList<WhatsAppTemplateVariable> variables,
        string? imagesUrl = null,
        string? facilityName = null)
    {
        var result = new Dictionary<int, string>();

        foreach (var v in variables)
        {
            result[v.Position] = v.Tag switch
            {
                WhatsAppTemplateTags.AccessionNumber => study.AccessionNumber ?? "",
                WhatsAppTemplateTags.PatientCode => patient?.PatientDicomId.Value ?? study.PatientId ?? "",
                WhatsAppTemplateTags.PatientName => CleanPatientName(study.PatientName ?? patient?.PatientName),
                WhatsAppTemplateTags.StudyDate => study.StudyDate?.ToString("dd/MM/yyyy") ?? "",
                WhatsAppTemplateTags.AppointmentDate => study.WorklistReadAt?.ToString("dd/MM/yyyy") ?? "",
                WhatsAppTemplateTags.StudyDescription => study.StudyDescription ?? "",
                WhatsAppTemplateTags.Modality => study.Series.FirstOrDefault()?.Modality ?? study.WorklistReadByModality ?? "",
                WhatsAppTemplateTags.ReferringPhysician => study.ReferringPhysician ?? "",
                WhatsAppTemplateTags.InstitutionName => facilityName ?? "",
                WhatsAppTemplateTags.PacsViewerLink => "",
                WhatsAppTemplateTags.ImagesUrl => SanitizeUrl(imagesUrl),
                WhatsAppTemplateTags.ImagesUrlPath => ToRelativePath(imagesUrl),
                _ => ""
            };
        }

        return result;
    }

    /// <summary>
    /// Converts a raw DICOM PN value (component groups delimited by <c>^</c>, e.g.
    /// <c>RONAL^MESSI^^^^</c>) into a human-readable name (<c>RONAL MESSI</c>) by replacing
    /// carets with spaces and collapsing surrounding/duplicate whitespace.
    /// </summary>
    internal static string CleanPatientName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";
        var parts = name.Split(['^', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', parts);
    }

    /// <summary>
    /// Percent-encodes characters that are illegal in a URI (e.g. spaces) so the value is safe to
    /// place in a WhatsApp template variable — including URL-button parameters, which Meta rejects
    /// when the resolved URL is malformed. Already percent-encoded sequences are left intact.
    /// </summary>
    internal static string SanitizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsoluteUri
            : url.Replace(" ", "%20");
    }

    /// <summary>
    /// Returns the path + query (+ fragment) of an absolute URL, without the scheme/host and
    /// without a leading slash — e.g. <c>http://host/Integrator.aspx?a=b</c> → <c>Integrator.aspx?a=b</c>.
    /// Intended for WhatsApp URL buttons whose template already contains the fixed domain
    /// (<c>http://host/{{n}}</c>), so the value is only the dynamic suffix. Already percent-encoded
    /// (illegal chars such as spaces are escaped). Falls back to <see cref="SanitizeUrl"/> when the
    /// input is not an absolute URL.
    /// </summary>
    internal static string ToRelativePath(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? (uri.PathAndQuery + uri.Fragment).TrimStart('/')
            : SanitizeUrl(url);
    }
}
