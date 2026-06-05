using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Application.Constants;
using Dicom.Edge.Hub.Application.Reports;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Syncs study records from HL7 messages.
///
/// ORM^O01 (no MRG) — Creates a <see cref="StudyStatus.Scheduled"/> study keyed by AccessionNumber.
///
/// ORM^O01 + MRG    — Reassigns the prior study (MRG.3 or prior patient's studies) to the
///                    surviving patient. If the prior study does not exist yet the current study
///                    is created normally.
///
/// ORU^R01           — Locates the study by AccessionNumber and attaches image links from OBX
///                    segments where OBX-2 is "RP"/"ED" or OBX-5 contains an http/wado URL.
///                    If the study doesn't exist yet it is created with status Receiving so the
///                    link is not lost when images eventually arrive.
/// </summary>
public sealed class Hl7StudySyncService(
    IStudyRepository studyRepository,
    IReportStorage reportStorage,
    IOptions<HubWorkspaceOptions> workspaceOptions,
    IUnitOfWork unitOfWork,
    ILogger<Hl7StudySyncService> logger) : IHl7StudySyncService
{
    public async Task SyncFromHl7Async(Hl7Message message, CancellationToken ct = default)
    {
        var baseType = message.MessageType.Split('^').FirstOrDefault() ?? message.MessageType;

        if (string.Equals(baseType, Hl7ValidationConstants.OrmMessageType, StringComparison.OrdinalIgnoreCase))
        {
            await HandleOrmAsync(message, ct);
            return;
        }

        if (string.Equals(baseType, Hl7ValidationConstants.OruMessageType, StringComparison.OrdinalIgnoreCase))
        {
            await HandleOruAsync(message, ct);
        }
    }

    // ── ORM^O01 — Order / Scheduled Study ────────────────────────────────────

    private async Task HandleOrmAsync(Hl7Message message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.AccessionNumber))
        {
            logger.LogInformation(
                "ORM {MessageId}: no AccessionNumber — study sync skipped",
                message.Id);
            return;
        }

        // ORM + MRG: consolidate prior accession into the current one
        if (message.HasMrgSegment && !string.IsNullOrWhiteSpace(message.MrgPriorAccessionNumber))
        {
            await HandleOrmMrgStudyMergeAsync(message, ct);
            return;
        }

        // Normal ORM: create study if not already present
        var existing = await studyRepository.GetByAccessionNumberAsync(message.AccessionNumber, ct);
        if (existing is not null)
        {
            logger.LogDebug(
                "ORM {MessageId}: study {StudyId} (AccessionNumber={AccessionNumber}) already exists — skipped",
                message.Id, existing.Id, message.AccessionNumber);
            return;
        }

        DateTime? studyDate = ParseStudyDate(message.StudyDate);

        var study = Study.CreateFromWorklist(
            accessionNumber:    message.AccessionNumber,
            patientId:          message.PatientId,
            patientName:        message.PatientName,
            sendingFacility:    message.SendingFacility,
            studyDate:          studyDate,
            studyDescription:   message.ProcedureDescription,
            referringPhysician: null);

        await studyRepository.AddAsync(study, ct);

        logger.LogInformation(
            "ORM {MessageId}: created scheduled study {StudyId} (AccessionNumber={AccessionNumber})",
            message.Id, study.Id, message.AccessionNumber);
    }

    // ── ORM + MRG — Study Merge / Reassignment ───────────────────────────────

    private async Task HandleOrmMrgStudyMergeAsync(Hl7Message message, CancellationToken ct)
    {
        var priorAccession    = message.MrgPriorAccessionNumber!;
        var currentAccession  = message.AccessionNumber!;

        var priorStudy = await studyRepository.GetByAccessionNumberAsync(priorAccession, ct);

        if (priorStudy is not null)
        {
            // Update the prior study to use the authoritative accession number and patient
            priorStudy.UpdateMetadata(accessionNumber: currentAccession);

            if (!string.IsNullOrWhiteSpace(message.PatientId))
                priorStudy.ReassignToPatient(message.PatientId, message.PatientName);

            await studyRepository.UpdateAsync(priorStudy, ct);

            logger.LogInformation(
                "ORM+MRG {MessageId}: study {StudyId} AccessionNumber updated {Prior} → {Current}",
                message.Id, priorStudy.Id, priorAccession, currentAccession);
            return;
        }

        // Prior study not found — check if current study already exists
        var currentStudy = await studyRepository.GetByAccessionNumberAsync(currentAccession, ct);
        if (currentStudy is not null)
        {
            logger.LogDebug(
                "ORM+MRG {MessageId}: prior study not found but current study {StudyId} already exists",
                message.Id, currentStudy.Id);
            return;
        }

        // Neither exists — create fresh
        var study = Study.CreateFromWorklist(
            accessionNumber:  currentAccession,
            patientId:        message.PatientId,
            patientName:      message.PatientName,
            sendingFacility:  message.SendingFacility,
            studyDate:        ParseStudyDate(message.StudyDate),
            studyDescription: message.ProcedureDescription);

        await studyRepository.AddAsync(study, ct);

        logger.LogInformation(
            "ORM+MRG {MessageId}: created study {StudyId} (prior AccessionNumber={Prior} not found)",
            message.Id, study.Id, priorAccession);
    }

    // ── ORU^R01 — Observation Result with Image Links ─────────────────────────

    private async Task HandleOruAsync(Hl7Message message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.AccessionNumber))
        {
            logger.LogInformation("ORU {MessageId}: no AccessionNumber — result sync skipped", message.Id);
            return;
        }

        var hasLinks  = message.ImageLinks.Count > 0;
        var hasReport = !string.IsNullOrWhiteSpace(message.ReportText);
        // Re-extract the PDF from the persisted raw content (the base64 PHI is never
        // stored as a field — it is decoded and written to the workspace here).
        var pdfBase64 = Hl7Message.ExtractReportPdfBase64(message.Content);
        var hasPdf    = !string.IsNullOrEmpty(pdfBase64);

        if (!hasLinks && !hasReport && !hasPdf)
        {
            logger.LogInformation(
                "ORU {MessageId}: no image links, report text or PDF in OBX — nothing to attach",
                message.Id);
            return;
        }

        var existing = await studyRepository.GetByAccessionNumberAsync(message.AccessionNumber, ct);

        // Study not yet in Hub (ORU arrived before ORM/DICOM) → create a stub so the
        // results are persisted and matched when images eventually arrive.
        var study = existing ?? Study.CreateFromWorklist(
            accessionNumber:  message.AccessionNumber,
            patientId:        message.PatientId,
            patientName:      message.PatientName,
            sendingFacility:  message.SendingFacility,
            studyDate:        ParseStudyDate(message.StudyDate),
            studyDescription: message.ProcedureDescription);

        if (hasLinks)
            study.AttachImageLinks(message.ImageLinks);

        string? pdfPath = null;
        if (hasPdf)
        {
            var bytes = DecodeReportPdf(pdfBase64!, message.Id);
            if (bytes is not null)
                pdfPath = await reportStorage.SavePdfAsync(study.Id, bytes, ct);
        }

        if (hasReport || pdfPath is not null)
            study.AttachReport(message.ReportFormat, message.ReportText, pdfPath);

        if (existing is null)
            await studyRepository.AddAsync(study, ct);
        else
            await studyRepository.UpdateAsync(study, ct);

        logger.LogInformation(
            "ORU {MessageId}: study {StudyId} (Accession={Accession}) links={Links} report={Report} pdf={Pdf} → {Status}",
            message.Id, study.Id, message.AccessionNumber, hasLinks, hasReport, pdfPath is not null, study.Status);
    }

    /// <summary>Decodes the base64 report PDF, enforcing the configured size limit.</summary>
    private byte[]? DecodeReportPdf(string base64, Guid messageId)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            var maxBytes = (long)workspaceOptions.Value.MaxPdfMb * 1024 * 1024;
            if (bytes.LongLength > maxBytes)
            {
                logger.LogWarning(
                    "ORU {MessageId}: report PDF {Bytes}B exceeds limit {Max}MB — skipped",
                    messageId, bytes.LongLength, workspaceOptions.Value.MaxPdfMb);
                return null;
            }

            // Sanity check: PDF magic header "%PDF".
            if (bytes.Length < 5 || bytes[0] != 0x25 || bytes[1] != 0x50 || bytes[2] != 0x44 || bytes[3] != 0x46)
                logger.LogWarning("ORU {MessageId}: decoded report does not start with %PDF — storing anyway", messageId);

            return bytes;
        }
        catch (FormatException)
        {
            logger.LogWarning("ORU {MessageId}: invalid base64 report PDF — skipped", messageId);
            return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DateTime? ParseStudyDate(string? hl7Date)
    {
        if (string.IsNullOrWhiteSpace(hl7Date)) return null;

        if (DateTime.TryParseExact(
                hl7Date[..Math.Min(14, hl7Date.Length)],
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        if (hl7Date.Length >= 8 && DateTime.TryParseExact(
                hl7Date[..8], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dateOnly))
            return DateTime.SpecifyKind(dateOnly, DateTimeKind.Utc);

        return null;
    }
}
