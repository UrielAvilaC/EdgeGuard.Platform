using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dicom.Edge.Abstractions.Equipment;
using Dicom.Edge.Node.Worklist;
using FellowOakDicom;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Handles DICOM Modality Worklist (MWL) C-FIND queries by matching the incoming
/// query keys against the local <see cref="IWorklistManager"/> store and mapping
/// each <see cref="WorklistItem"/> to a standard DICOM MWL response dataset.
/// The result is constrained per calling equipment: only worklist items whose modality
/// is in the equipment's allowed set (and, when configured, whose scheduled station AE
/// matches) are returned.
/// </summary>
public sealed class WorklistCFindHandler(
    IWorklistManager worklistManager,
    IEquipmentCatalog equipmentCatalog,
    ILogger<WorklistCFindHandler> logger) : IWorklistCFindHandler
{
    /// <inheritdoc />
    public async IAsyncEnumerable<DicomDataset> QueryWorklistAsync(
        DicomDataset queryKeys,
        string callingAe,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // ── Extract query filters from the DICOM C-FIND request dataset ──
        var patientId = queryKeys.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
        var patientName = queryKeys.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
        var accessionNumber = queryKeys.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);

        string modality = queryKeys.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
        string scheduledDate = string.Empty;
        string scheduledStationAe = string.Empty;

        if (queryKeys.Contains(DicomTag.ScheduledProcedureStepSequence))
        {
            var spsSeq = queryKeys.GetSequence(DicomTag.ScheduledProcedureStepSequence);
            if (spsSeq.Items.Count > 0)
            {
                var spsItem = spsSeq.Items[0];
                modality = spsItem.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);
                if (!string.IsNullOrEmpty(modality))
                {
                    modality = modality.Trim('*');
                }
                scheduledDate = spsItem.GetSingleValueOrDefault(
                    DicomTag.ScheduledProcedureStepStartDate, string.Empty);
                scheduledStationAe = spsItem.GetSingleValueOrDefault(
                    DicomTag.ScheduledStationAETitle, string.Empty);
            }
        }

        // ── Parse DICOM date filter into from/to for DB-level pre-filtering ──
        var (dateFrom, dateTo) = ParseDicomDateRange(scheduledDate);

        // ── Per-equipment constraint: resolve the calling equipment from the catalog ──
        var equipment = equipmentCatalog.FindByAeTitle(callingAe);

        // When the catalog is populated, the association layer has already rejected
        // unknown/disabled equipment. Defensive guard for the race where MWL is queried
        // by an AE with no catalog entry while the catalog is populated.
        if (equipment is null && !equipmentCatalog.IsEmpty)
        {
            logger.LogWarning(
                "MWL C-FIND from uncatalogued AE '{CallingAe}' — returning empty worklist", callingAe);
            yield break;
        }

        // null  => no per-equipment constraint (empty catalog → legacy behaviour)
        // empty => equipment exists but has no modalities assigned → sees nothing
        var allowedModalities = equipment?.ModalityCodes;
        var equipmentStationAe = equipment?.StationAeTitle;

        if (allowedModalities is { Count: 0 })
        {
            logger.LogInformation(
                "MWL C-FIND: equipment '{CallingAe}' has no modalities assigned — empty worklist", callingAe);
            yield break;
        }

        // Resolve the modality to push to the DB-level filter, honouring the equipment's
        // allowed set. When the device requested a modality outside its allowed set, stop.
        string? dbModality = string.IsNullOrEmpty(modality) ? null : modality;

        if (allowedModalities is { Count: > 0 })
        {
            if (!string.IsNullOrEmpty(modality))
            {
                if (!allowedModalities.Contains(modality, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogInformation(
                        "MWL C-FIND: requested modality '{Modality}' is not allowed for '{CallingAe}' — empty worklist",
                        modality, callingAe);
                    yield break;
                }
                dbModality = modality;
            }
            else if (allowedModalities.Count == 1)
            {
                // Single allowed modality — push it to the DB filter for efficiency.
                dbModality = allowedModalities[0];
            }
            // else: multiple allowed modalities, no device filter → filter in-memory below.
        }

        logger.LogDebug(
            "MWL C-FIND query — CallingAE={CallingAe}, PatientId={PatientId}, PatientName={PatientName}, " +
            "AccessionNumber={AccessionNumber}, Modality={Modality}, AllowedModalities={Allowed}, " +
            "ScheduledDate={Date}, StationAE={StationAe}",
            callingAe, patientId, patientName, accessionNumber, modality,
            allowedModalities is null ? "(any)" : string.Join("/", allowedModalities),
            scheduledDate, scheduledStationAe);

        // Push modality and date filters to the DB; remaining filters are applied in-memory.
        var items = await worklistManager.QueryAsync(dateFrom, dateTo, dbModality, ct);

        int matchCount = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (!MatchesQueryKeys(item, patientId, patientName,
                    accessionNumber, scheduledStationAe))
                continue;

            if (!MatchesEquipment(item, allowedModalities, equipmentStationAe))
                continue;

            yield return BuildResponseDataset(item);
            matchCount++;
        }

        logger.LogInformation("MWL C-FIND completed — {MatchCount} matches returned", matchCount);

        // ── MWL-FIX-1 ─────────────────────────────────────────────────────
        // PREVIOUSLY: items returned by C-FIND were marked as "queried" so they
        // would not appear again. This broke standard MWL usage where modalities
        // legitimately re-query the worklist multiple times per shift.
        //
        // The correct state transition is: `pending` → `queried` (or `completed`)
        // when the corresponding DICOM images arrive via C-STORE (matched by
        // AccessionNumber). That transition is owned by the C-STORE handler /
        // study completion pipeline, NOT by C-FIND.
        // ──────────────────────────────────────────────────────────────────
    }

    // ── Per-equipment matching ──────────────────────────────────────────────

    /// <summary>
    /// Enforces the per-equipment constraint on a worklist item: the item's modality must
    /// be in the equipment's allowed set, and (when the equipment declares a station AE)
    /// the item's scheduled station AE must match. Items without a station AE are treated
    /// leniently (HL7 ORM frequently omits OBR-21/22) — mirroring the query-key behaviour.
    /// </summary>
    private static bool MatchesEquipment(
        WorklistItem item,
        IReadOnlyList<string>? allowedModalities,
        string? equipmentStationAe)
    {
        if (allowedModalities is { Count: > 0 })
        {
            if (string.IsNullOrEmpty(item.Modality) ||
                !allowedModalities.Contains(item.Modality.Trim(), StringComparer.OrdinalIgnoreCase))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(equipmentStationAe) &&
            !string.IsNullOrWhiteSpace(item.ScheduledStationAeTitle) &&
            !string.Equals(
                item.ScheduledStationAeTitle.Trim(),
                equipmentStationAe.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    // ── Query matching ──────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates whether a worklist item matches the remaining in-memory C-FIND query keys.
    /// Modality and scheduled date are pre-filtered at the DB level by <see cref="IWorklistManager.QueryAsync"/>.
    /// Empty/null query values act as universal matches per the DICOM standard.
    /// Supports DICOM wildcards (* and ? mapped to .* and . in regex).
    /// </summary>
    private static bool MatchesQueryKeys(
        WorklistItem item,
        string patientId,
        string patientName,
        string accessionNumber,
        string scheduledStationAe)
    {
        // Query values are trimmed because DICOM CS / SH / AE VRs pad with trailing
        // spaces; some SCUs do not strip them before sending the C-FIND request.
        patientId = patientId?.Trim() ?? string.Empty;
        patientName = patientName?.Trim() ?? string.Empty;
        accessionNumber = accessionNumber?.Trim() ?? string.Empty;
        scheduledStationAe = scheduledStationAe?.Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(patientId) &&
            !WildcardMatch(item.PatientId, patientId))
            return false;

        if (!string.IsNullOrEmpty(patientName) &&
            !WildcardMatch(item.PatientName, patientName))
            return false;

        if (!string.IsNullOrEmpty(accessionNumber) &&
            !WildcardMatch(item.AccessionNumber, accessionNumber))
            return false;

        // MWL-FIX-2: When the item does not carry a ScheduledStationAeTitle
        // (HL7 ORM frequently omits OBR-21/22), treat the item as matching any
        // station AE filter from the modality. Strict equality on null would
        // silently drop every matching study, which is the original symptom.
        if (!string.IsNullOrEmpty(scheduledStationAe) &&
            !string.IsNullOrEmpty(item.ScheduledStationAeTitle) &&
            !string.Equals(
                item.ScheduledStationAeTitle.Trim(),
                scheduledStationAe,
                StringComparison.OrdinalIgnoreCase))
            return false;

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
    /// Parses a DICOM DA value into a <c>(from, to)</c> date range for DB-level filtering.
    /// Supports:
    /// <list type="bullet">
    ///   <item>Single date: <c>YYYYMMDD</c> → same day range.</item>
    ///   <item>Bounded range: <c>YYYYMMDD-YYYYMMDD</c></item>
    ///   <item>Open start: <c>-YYYYMMDD</c> → <c>(null, to)</c></item>
    ///   <item>Open end: <c>YYYYMMDD-</c> → <c>(from, null)</c></item>
    /// </list>
    /// </summary>
    private static (DateTime? from, DateTime? to) ParseDicomDateRange(string dicomDate)
    {
        if (string.IsNullOrWhiteSpace(dicomDate))
            return (null, null);

        // MWL-FIX-4: end-of-day (23:59:59.9999999) on the upper bound so that
        // items persisted with a non-midnight ScheduledDate (e.g., when EF/SQLite
        // round-trip introduces sub-second precision) still match a "today" query.
        if (dicomDate.Contains('-'))
        {
            var parts = dicomDate.Split('-', 2);
            var from = ParseSingleDicomDate(parts[0]);
            var to = EndOfDay(ParseSingleDicomDate(parts[1]));
            return (from, to);
        }

        var single = ParseSingleDicomDate(dicomDate);
        return (single, EndOfDay(single));
    }

    private static DateTime? EndOfDay(DateTime? value) =>
        value.HasValue ? value.Value.Date.AddDays(1).AddTicks(-1) : null;

    /// <summary>Parses a single DICOM DA string <c>YYYYMMDD</c>; returns <c>null</c> on failure.</summary>
    private static DateTime? ParseSingleDicomDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
            return null;

        return DateTime.TryParseExact(
            value[..8], "yyyyMMdd",
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
