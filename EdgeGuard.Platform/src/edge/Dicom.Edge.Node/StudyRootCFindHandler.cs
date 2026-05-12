using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dicom.Edge.Models.Core;
using Dicom.Edge.Node.DicomServer;
using Dicom.Edge.Node.Persistence.Context;
using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node;

/// <summary>
/// Resolves Study Root Query/Retrieve C-FIND requests against the local study database.
/// Supports basic DICOM query keys such as PatientID, PatientName, StudyInstanceUID,
/// AccessionNumber, and StudyDate (single date or range).
/// </summary>
internal sealed class StudyRootCFindHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<StudyRootCFindHandler> logger) : IStudyRootCFindHandler
{
    public async IAsyncEnumerable<DicomDataset> QueryStudiesAsync(
        DicomDataset queryKeys,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var studyInstanceUid = queryKeys.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty).Trim();
        var patientId = queryKeys.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty).Trim();
        var patientName = queryKeys.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty).Trim();
        var accessionNumber = queryKeys.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty).Trim();
        var studyDate = queryKeys.GetSingleValueOrDefault(DicomTag.StudyDate, string.Empty).Trim();

        logger.LogDebug(
            "Study Root C-FIND query — StudyInstanceUID={StudyInstanceUid}, PatientId={PatientId}, " +
            "PatientName={PatientName}, AccessionNumber={AccessionNumber}, StudyDate={StudyDate}",
            studyInstanceUid, patientId, patientName, accessionNumber, studyDate);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EdgeNodeDbContext>();

        IQueryable<DicomStudy> query = db.Studies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(studyInstanceUid))
            query = query.Where(s => s.StudyInstanceUid == studyInstanceUid);

        if (!string.IsNullOrWhiteSpace(patientId) && !HasWildcard(patientId))
            query = query.Where(s => s.PatientId == patientId);

        if (!string.IsNullOrWhiteSpace(patientName) && !HasWildcard(patientName))
            query = query.Where(s => s.PatientName == patientName);

        if (!string.IsNullOrWhiteSpace(accessionNumber) && !HasWildcard(accessionNumber))
            query = query.Where(s => s.AccessionNumber == accessionNumber);

        if (TryParseDicomDateRange(studyDate, out var fromDate, out var toDate))
        {
            if (fromDate.HasValue)
                query = query.Where(s => s.StudyDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(s => s.StudyDate <= toDate.Value);
        }

        var studies = await query
            .OrderByDescending(s => s.StudyDate)
            .ToListAsync(ct);

        var resultCount = 0;
        foreach (var study in studies)
        {
            ct.ThrowIfCancellationRequested();

            if (!WildcardMatch(study.PatientId, patientId) ||
                !WildcardMatch(study.PatientName, patientName) ||
                !WildcardMatch(study.AccessionNumber, accessionNumber) ||
                !MatchesDicomDate(study.StudyDate, studyDate))
            {
                continue;
            }

            yield return BuildStudyRootResponse(study);
            resultCount++;
        }

        logger.LogInformation(
            "Study Root C-FIND completed — {MatchCount} matches returned",
            resultCount);
    }

    private static DicomDataset BuildStudyRootResponse(DicomStudy study)
    {
        var ds = new DicomDataset();
        ds.AddOrUpdate(DicomTag.QueryRetrieveLevel, "STUDY");
        ds.AddOrUpdate(DicomTag.StudyInstanceUID, study.StudyInstanceUid ?? string.Empty);
        ds.AddOrUpdate(DicomTag.PatientID, study.PatientId ?? string.Empty);
        ds.AddOrUpdate(DicomTag.PatientName, study.PatientName ?? string.Empty);
        ds.AddOrUpdate(DicomTag.StudyDate, study.StudyDate.ToString("yyyyMMdd"));
        ds.AddOrUpdate(DicomTag.AccessionNumber, study.AccessionNumber ?? string.Empty);
        ds.AddOrUpdate(DicomTag.StudyDescription, study.StudyDescription ?? string.Empty);
        ds.AddOrUpdate(DicomTag.ReferringPhysicianName, study.ReferringPhysician ?? string.Empty);
        ds.AddOrUpdate(DicomTag.NumberOfStudyRelatedInstances, study.InstanceCount);
        return ds;
    }

    private static bool HasWildcard(string value) =>
        value.Contains('*') || value.Contains('?');

    private static bool WildcardMatch(string? value, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return true;

        if (string.IsNullOrEmpty(value))
            return false;

        if (!HasWildcard(pattern))
            return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

        var regexPattern = "^" +
            Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") +
            "$";

        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }

    private static bool MatchesDicomDate(DateTime studyDate, string dicomDate)
    {
        if (string.IsNullOrWhiteSpace(dicomDate))
            return true;

        if (!TryParseDicomDateRange(dicomDate, out var fromDate, out var toDate))
            return false;

        if (fromDate.HasValue && studyDate.Date < fromDate.Value.Date)
            return false;
        if (toDate.HasValue && studyDate.Date > toDate.Value.Date)
            return false;

        return true;
    }

    private static bool TryParseDicomDateRange(
        string dicomDate,
        out DateTime? fromDate,
        out DateTime? toDate)
    {
        fromDate = null;
        toDate = null;

        if (string.IsNullOrWhiteSpace(dicomDate))
            return false;

        if (!dicomDate.Contains('-'))
        {
            if (!TryParseDicomDate(dicomDate, out var exact))
                return false;

            fromDate = exact.Date;
            toDate = exact.Date;
            return true;
        }

        var parts = dicomDate.Split('-', 2);

        if (!string.IsNullOrWhiteSpace(parts[0]) && TryParseDicomDate(parts[0], out var start))
            fromDate = start.Date;

        if (parts.Length > 1 &&
            !string.IsNullOrWhiteSpace(parts[1]) &&
            TryParseDicomDate(parts[1], out var end))
            toDate = end.Date;

        return fromDate.HasValue || toDate.HasValue;
    }

    private static bool TryParseDicomDate(string value, out DateTime date) =>
        DateTime.TryParseExact(
            value,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
}
