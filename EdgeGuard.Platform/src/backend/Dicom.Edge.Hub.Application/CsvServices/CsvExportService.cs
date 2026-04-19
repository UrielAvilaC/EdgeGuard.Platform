using System.Globalization;
using System.Text;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Application.CsvServices;

public interface ICsvExportService
{
    Task<CsvExportResultDto> ExportStudiesAsync(StudyFilter filter, CancellationToken ct = default);
    Task<CsvExportResultDto> ExportPatientsAsync(PatientFilter filter, CancellationToken ct = default);
}

public sealed class CsvExportService(
    IStudyRepository studyRepository,
    IPatientRepository patientRepository) : ICsvExportService
{
    public async Task<CsvExportResultDto> ExportStudiesAsync(StudyFilter filter, CancellationToken ct = default)
    {
        var criteria = new StudyFilterCriteria
        {
            Search = filter.Search,
            Status = filter.Status,
            SourceNodeId = filter.SourceNodeId,
            PatientId = filter.PatientId,
            DateFrom = filter.DateFrom,
            DateTo = filter.DateTo,
            IsUrgent = filter.IsUrgent,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };

        var studies = await studyRepository.GetFilteredAllAsync(criteria, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,StudyInstanceUid,AccessionNumber,PatientId,PatientName,Status,StudyDate,StudyDescription,SourceNodeId,InstanceCount,SeriesCount,TotalSizeBytes,IsUrgent,CreatedAt");

        foreach (var study in studies)
        {
            sb.AppendLine(string.Join(",",
                Escape(study.Id),
                Escape(study.StudyInstanceUid.Value),
                Escape(study.AccessionNumber),
                Escape(study.PatientId),
                Escape(study.PatientName),
                study.Status.ToString(),
                study.StudyDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Escape(study.StudyDescription),
                Escape(study.SourceNodeId),
                study.InstanceCount,
                study.Series.Count,
                study.TotalSizeBytes,
                study.IsUrgent,
                study.CreatedAt.ToString("O")));
        }

        return new CsvExportResultDto
        {
            FileContent = Encoding.UTF8.GetBytes(sb.ToString()),
            FileName = $"studies-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = studies.Count
        };
    }

    public async Task<CsvExportResultDto> ExportPatientsAsync(PatientFilter filter, CancellationToken ct = default)
    {
        var criteria = new PatientFilterCriteria
        {
            Search = filter.Search,
            CreatedByNodeId = filter.CreatedByNodeId,
            IsActive = filter.IsActive,
            HasPhone = filter.HasPhone,
            HasEmail = filter.HasEmail,
            SortBy = filter.SortBy,
            SortDir = filter.SortDir
        };

        var patients = await patientRepository.GetFilteredAllAsync(criteria, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,PatientDicomId,PatientName,BirthDate,Sex,PhoneNumber,Email,IsActive,CreatedByNodeId,CreatedAt");

        foreach (var p in patients)
        {
            sb.AppendLine(string.Join(",",
                Escape(p.Id),
                Escape(p.PatientDicomId.Value),
                Escape(p.PatientName),
                p.BirthDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Escape(p.Sex),
                Escape(p.PhoneNumber),
                Escape(p.Email),
                p.IsActive,
                Escape(p.CreatedByNodeId),
                p.CreatedAt.ToString("O")));
        }

        return new CsvExportResultDto
        {
            FileContent = Encoding.UTF8.GetBytes(sb.ToString()),
            FileName = $"patients-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv",
            ContentType = "text/csv",
            RecordCount = patients.Count
        };
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
