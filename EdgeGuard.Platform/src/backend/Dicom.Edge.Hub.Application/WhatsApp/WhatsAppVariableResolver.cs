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
                WhatsAppTemplateTags.PatientName => study.PatientName ?? patient?.PatientName ?? "",
                WhatsAppTemplateTags.StudyDate => study.StudyDate?.ToString("dd/MM/yyyy") ?? "",
                WhatsAppTemplateTags.AppointmentDate => study.WorklistReadAt?.ToString("dd/MM/yyyy") ?? "",
                WhatsAppTemplateTags.StudyDescription => study.StudyDescription ?? "",
                WhatsAppTemplateTags.Modality => study.Series.FirstOrDefault()?.Modality ?? study.WorklistReadByModality ?? "",
                WhatsAppTemplateTags.ReferringPhysician => study.ReferringPhysician ?? "",
                WhatsAppTemplateTags.InstitutionName => facilityName ?? "",
                WhatsAppTemplateTags.PacsViewerLink => "",
                WhatsAppTemplateTags.ImagesUrl => imagesUrl ?? "",
                _ => ""
            };
        }

        return result;
    }
}
