using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dicom.Edge.Node.Worklist;
using FellowOakDicom;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Handles DICOM Modality Worklist (MWL) C-FIND queries by matching the incoming
/// query keys against the local <see cref="IWorklistManager"/> store and mapping
/// each <see cref="WorklistItem"/> to a standard DICOM MWL response dataset.
/// </summary>
public sealed class WorklistCFindHandler(
    IWorklistManager worklistManager,
    ILogger<WorklistCFindHandler> logger) : IWorklistCFindHandler
{
    /// <inheritdoc />
    public async IAsyncEnumerable<DicomDataset> QueryWorklistAsync(
        DicomDataset queryKeys,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // ── Extract query filters from the DICOM C-FIND request dataset ──
        var patientId = queryKeys.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
        var patientName = queryKeys.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
        var accessionNumber = queryKeys.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);

        string modality = string.Empty;
        string scheduledDate = string.Empty;
        string scheduledStationAe = string.Empty;

        if (queryKeys.Contains(DicomTag.ScheduledProcedureStepSequence))
        {
            var spsSeq = queryKeys.GetSequence(DicomTag.ScheduledProcedureStepSequence);
            if (spsSeq.Items.Count > 0)
            {
                var spsItem = spsSeq.Items[0];
                modality = spsItem.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
                scheduledDate = spsItem.GetSingleValueOrDefault(
                    DicomTag.ScheduledProcedureStepStartDate, string.Empty);
                scheduledStationAe = spsItem.GetSingleValueOrDefault(
                    DicomTag.ScheduledStationAETitle, string.Empty);
            }
        }

        logger.LogDebug(
            "MWL C-FIND query — PatientId={PatientId}, PatientName={PatientName}, " +
            "AccessionNumber={AccessionNumber}, Modality={Modality}, " +
            "ScheduledDate={Date}, StationAE={StationAe}",
            patientId, patientName, accessionNumber, modality, scheduledDate, scheduledStationAe);

        var items = await worklistManager.GetActiveItemsAsync(ct);
        int matchCount = 0;
        var queriedIds = new List<string>();

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (!MatchesQueryKeys(item, patientId, patientName,
                    accessionNumber, modality, scheduledDate, scheduledStationAe))
                continue;

            if (!string.IsNullOrEmpty(item.AccessionNumber))
                queriedIds.Add(item.AccessionNumber);

            yield return BuildResponseDataset(item);
            matchCount++;
        }

        logger.LogInformation("MWL C-FIND completed — {MatchCount} matches returned", matchCount);

        if (queriedIds.Count > 0)
            _ = worklistManager.MarkItemsAsQueriedAsync(queriedIds, ct);
    }

    // ── Query matching ──────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates whether a worklist item matches the DICOM C-FIND query keys.
    /// Empty/null query values act as universal matches per the DICOM standard.
    /// Supports DICOM wildcards (* and ? mapped to .* and . in regex).
    /// </summary>
    private static bool MatchesQueryKeys(
        WorklistItem item,
        string patientId,
        string patientName,
        string accessionNumber,
        string modality,
        string scheduledDate,
        string scheduledStationAe)
    {
        if (!string.IsNullOrEmpty(patientId) &&
            !WildcardMatch(item.PatientId, patientId))
            return false;

        if (!string.IsNullOrEmpty(patientName) &&
            !WildcardMatch(item.PatientName, patientName))
            return false;

        if (!string.IsNullOrEmpty(accessionNumber) &&
            !WildcardMatch(item.AccessionNumber, accessionNumber))
            return false;

        if (!string.IsNullOrEmpty(modality) &&
            !string.Equals(item.Modality, modality, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(scheduledStationAe) &&
            !string.Equals(item.ScheduledStationAeTitle, scheduledStationAe, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(scheduledDate) && item.ScheduledDateTime.HasValue)
        {
            var queryDate = ParseDicomDate(scheduledDate);
            if (queryDate.HasValue && item.ScheduledDateTime.Value.Date != queryDate.Value.Date)
                return false;
        }

        return true;
    }

    // ── Response dataset construction ───────────────────────────────────────

    /// <summary>
    /// Builds a complete DICOM MWL response dataset from a <see cref="WorklistItem"/>.
    /// Populates Patient, Study, Requested Procedure, and Scheduled Procedure Step attributes.
    /// </summary>
    private static DicomDataset BuildResponseDataset(WorklistItem item)
    {
        var ds = new DicomDataset();

        // ── Patient Level ────────────────────────────────────────────────
        ds.AddOrUpdate(DicomTag.PatientName, item.PatientName ?? string.Empty);
        ds.AddOrUpdate(DicomTag.PatientID, item.PatientId ?? string.Empty);
        ds.AddOrUpdate(DicomTag.PatientBirthDate, item.PatientBirthDate ?? string.Empty);
        ds.AddOrUpdate(DicomTag.PatientSex, item.PatientSex ?? string.Empty);

        // ── Study / Requested Procedure ──────────────────────────────────
        ds.AddOrUpdate(DicomTag.StudyInstanceUID,
            item.StudyInstanceUid ?? DicomUIDGenerator.GenerateDerivedFromUUID().UID);
        ds.AddOrUpdate(DicomTag.AccessionNumber, item.AccessionNumber ?? string.Empty);
        ds.AddOrUpdate(DicomTag.ReferringPhysicianName,
            item.ReferringPhysicianName ?? string.Empty);
        ds.AddOrUpdate(DicomTag.RequestedProcedureDescription,
            item.ProcedureDescription ?? string.Empty);
        ds.AddOrUpdate(DicomTag.RequestedProcedureID,
            item.RequestedProcedureId ?? item.Id);

        // ── Scheduled Procedure Step Sequence (0040,0100) ────────────────
        var spsDataset = new DicomDataset();
        spsDataset.AddOrUpdate(DicomTag.ScheduledStationAETitle,
            item.ScheduledStationAeTitle ?? string.Empty);
        spsDataset.AddOrUpdate(DicomTag.Modality,
            item.Modality ?? string.Empty);
        spsDataset.AddOrUpdate(DicomTag.ScheduledPerformingPhysicianName,
            item.ScheduledPerformingPhysicianName ?? string.Empty);
        spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepDescription,
            item.ProcedureDescription ?? string.Empty);
        spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepID,
            item.ScheduledProcedureStepId ?? item.Id);

        if (item.ScheduledDateTime.HasValue)
        {
            spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate,
                item.ScheduledDateTime.Value.ToString("yyyyMMdd"));
            spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartTime,
                item.ScheduledDateTime.Value.ToString("HHmmss"));
        }
        else
        {
            spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, string.Empty);
            spsDataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartTime, string.Empty);
        }

        ds.AddOrUpdate(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, spsDataset));

        return ds;
    }

    // ── Utility helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Parses a DICOM DA (date) value — YYYYMMDD or range YYYYMMDD-YYYYMMDD.
    /// Returns the first date component for single-value matching.
    /// </summary>
    private static DateTime? ParseDicomDate(string dicomDate)
    {
        if (string.IsNullOrWhiteSpace(dicomDate))
            return null;

        // Handle range format: take the first date
        var datePart = dicomDate.Contains('-')
            ? dicomDate.Split('-')[0]
            : dicomDate;

        if (datePart.Length < 8)
            return null;

        return DateTime.TryParseExact(
            datePart[..8], "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var result)
            ? result
            : null;
    }

    /// <summary>
    /// DICOM wildcard matching — '*' matches any sequence of characters,
    /// '?' matches any single character. Comparison is case-insensitive.
    /// </summary>
    private static bool WildcardMatch(string? value, string pattern)
    {
        if (string.IsNullOrEmpty(value))
            return string.IsNullOrEmpty(pattern) || pattern == "*";

        var regexPattern = "^" +
            Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") +
            "$";

        return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
    }
}
