using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Study aggregate root. Tracks the full lifecycle of a DICOM study in the Hub.
/// </summary>
public sealed class Study : AggregateRoot<string>, ISoftDeletable
{
    private readonly List<StudySeries> _series = [];
    private readonly List<StudyStatusAudit> _statusAudits = [];

    // DICOM identifiers
    public DicomUid StudyInstanceUid { get; private set; } = default!;
    public string? AccessionNumber { get; private set; }
    public DateTime? StudyDate { get; private set; }
    public string? StudyDescription { get; private set; }
    public string? ReferringPhysician { get; private set; }

    // Patient reference
    public string? PatientId { get; private set; }
    public string? PatientName { get; private set; }

    // Origin tracking
    public string? SourceNodeId { get; private set; }
    public string? SourceAeTitle { get; private set; }
    public string? ReceivingAeTitle { get; private set; }

    // Status tracking
    public StudyStatus Status { get; private set; }
    public DateTime CurrentStatusSince { get; private set; }

    // Image tracking
    public int InstanceCount { get; private set; }
    public int SeriesCount { get; private set; }
    public long TotalSizeBytes { get; private set; }
    public DateTime? FirstImageReceivedAt { get; private set; }
    public DateTime? LastImageReceivedAt { get; private set; }

    // Worklist tracking
    public string? WorklistReadByModality { get; private set; }
    public DateTime? WorklistReadAt { get; private set; }

    // PACS send tracking
    public string? TargetPacsId { get; private set; }
    public DateTime? SentToPacsAt { get; private set; }
    public int PacsSendAttempts { get; private set; }
    public string? PacsSendLastError { get; private set; }

    // External image/report links (from ORU^R01 OBX segments)
    /// <summary>Newline-separated URLs to external image viewers or report portals, populated from ORU OBX-5 RP values.</summary>
    public string? ExternalImageLinks { get; private set; }

    // Diagnostic report (from ORU^R01 OBX TX/FT segments and/or ED PDF)
    /// <summary>Format of <see cref="ReportContent"/> (None when only a PDF or no report).</summary>
    public ReportFormat ReportFormat { get; private set; } = ReportFormat.None;
    /// <summary>The textual/HTML report body extracted from ORU OBX TX/FT segments.</summary>
    public string? ReportContent { get; private set; }
    /// <summary>Relative path (within the Hub workspace) to the report PDF, if any.</summary>
    public string? ReportPdfPath { get; private set; }
    /// <summary>When the first report artifact (text/HTML/PDF) was attached.</summary>
    public DateTime? ReportReceivedAt { get; private set; }

    /// <summary>True when a diagnostic report (text/HTML or PDF) is present.</summary>
    public bool HasReport => ReportFormat != ReportFormat.None || !string.IsNullOrEmpty(ReportPdfPath);
    /// <summary>True when at least one image link (liga de imágenes) is present.</summary>
    public bool HasImageLinks => !string.IsNullOrEmpty(ExternalImageLinks);

    // Enterprise
    public int Priority { get; private set; }
    public bool IsUrgent { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public IReadOnlyList<StudySeries> Series => _series.AsReadOnly();
    public IReadOnlyList<StudyStatusAudit> StatusAudits => _statusAudits.AsReadOnly();

    private Study() { }

    public static Study Create(
        DicomUid studyInstanceUid,
        string? patientId = null,
        string? patientName = null,
        string? sourceNodeId = null,
        string? sourceAeTitle = null,
        string? receivingAeTitle = null,
        string? accessionNumber = null,
        DateTime? studyDate = null,
        string? studyDescription = null,
        string? referringPhysician = null,
        int maxRetries = 3)
    {
        var study = new Study
        {
            Id = IdGenerator.NewId(),
            StudyInstanceUid = studyInstanceUid,
            PatientId = patientId,
            PatientName = patientName?.Trim(),
            SourceNodeId = sourceNodeId,
            SourceAeTitle = sourceAeTitle?.Trim(),
            ReceivingAeTitle = receivingAeTitle?.Trim(),
            AccessionNumber = accessionNumber?.Trim(),
            StudyDate = studyDate,
            StudyDescription = studyDescription?.Trim(),
            ReferringPhysician = referringPhysician?.Trim(),
            Status = StudyStatus.Receiving,
            CurrentStatusSince = DateTime.UtcNow,
            Priority = 5,
            MaxRetries = maxRetries
        };

        study.AddDomainEvent(new StudyReceivedEvent(
            study.Id, studyInstanceUid.Value, patientId, sourceNodeId));

        study.RecordStatusChange(null, StudyStatus.Receiving, sourceNodeId, "Study created");

        return study;
    }

    /// <summary>
    /// Creates a study from an HL7 worklist message (ORM/SIU).
    /// Generates a synthetic DICOM UID — will be replaced when real images arrive.
    /// Status is set to <see cref="StudyStatus.Scheduled"/>.
    /// </summary>
    public static Study CreateFromWorklist(
        string accessionNumber,
        string? patientId = null,
        string? patientName = null,
        string? sendingFacility = null,
        DateTime? studyDate = null,
        string? studyDescription = null,
        string? referringPhysician = null)
    {
        // Generate a synthetic UID: 2.25.<128-bit number from GUID>
        var uid = DicomUid.Create($"2.25.{BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0)}{BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0)}");

        var study = new Study
        {
            Id = IdGenerator.NewId(),
            StudyInstanceUid = uid,
            AccessionNumber = accessionNumber.Trim(),
            PatientId = patientId,
            PatientName = patientName?.Trim(),
            SourceNodeId = sendingFacility,
            StudyDate = studyDate,
            StudyDescription = studyDescription?.Trim(),
            ReferringPhysician = referringPhysician?.Trim(),
            Status = StudyStatus.Scheduled,
            CurrentStatusSince = DateTime.UtcNow,
            Priority = 5,
            MaxRetries = 3
        };

        study.RecordStatusChange(null, StudyStatus.Scheduled, sendingFacility, "Scheduled from HL7 worklist");
        study.AddDomainEvent(new StudyScheduledEvent(study.Id, study.AccessionNumber));

        return study;
    }

    /// <summary>
    /// Merges a real DICOM study arrival into a study that was pre-created from an HL7 worklist message.
    /// Replaces the synthetic <see cref="StudyInstanceUid"/> with the authoritative UID that arrived
    /// embedded in the DICOM instances, transitions the status to <see cref="StudyStatus.Receiving"/>,
    /// and enriches any origin fields that were unknown at scheduling time.
    /// </summary>
    /// <remarks>
    /// This is the resolution point for the ORM→DICOM UID mismatch:
    /// <list type="number">
    ///   <item>HL7 ORM arrives → study created with synthetic UID, keyed by AccessionNumber.</item>
    ///   <item>DICOM images arrive → Edge Node notifies Hub with real UID + same AccessionNumber.</item>
    ///   <item>Hub looks up by real UID (miss), falls back to AccessionNumber, calls this method.</item>
    /// </list>
    /// </remarks>
    public void MergeFromDicom(
        DicomUid realStudyInstanceUid,
        string? sourceNodeId = null,
        string? sourceAeTitle = null)
    {
        var previousUid = StudyInstanceUid.Value;

        StudyInstanceUid = realStudyInstanceUid;

        if (sourceNodeId is not null) SourceNodeId = sourceNodeId;
        if (sourceAeTitle is not null) SourceAeTitle = sourceAeTitle.Trim();

        var old = Status;
        Status = StudyStatus.Receiving;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, sourceNodeId,
            $"Merged: synthetic UID {previousUid} replaced by real DICOM UID {realStudyInstanceUid.Value}");

        AddDomainEvent(new StudyReceivedEvent(Id, realStudyInstanceUid.Value, PatientId, sourceNodeId));
    }

    public void MarkAsScheduled()
    {
        var old = Status;
        Status = StudyStatus.Scheduled;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RecordStatusChange(old, Status, null, "Re-scheduled");
        AddDomainEvent(new StudyScheduledEvent(Id, AccessionNumber));
    }

    /// <summary>
    /// Updates study-level metadata (date, description) when received from the node.
    /// Only overwrites if the incoming value is non-null.
    /// </summary>
    public void UpdateStudyMetadata(DateTime? studyDate, string? studyDescription)
    {
        if (studyDate.HasValue) StudyDate = studyDate;
        if (!string.IsNullOrWhiteSpace(studyDescription)) StudyDescription = studyDescription;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordImagesReceived(int count, long sizeBytes, string? modalityAeTitle = null, int seriesCount = 0)
    {
        if (count <= 0) return;

        InstanceCount += count;
        TotalSizeBytes += sizeBytes;
        LastImageReceivedAt = DateTime.UtcNow;
        FirstImageReceivedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        if (seriesCount > SeriesCount)
            SeriesCount = seriesCount;

        AddDomainEvent(new StudyImagesReceivedEvent(Id, count, sizeBytes, modalityAeTitle));
    }

    public void MarkCompleted()
    {
        var old = Status;
        Status = StudyStatus.Completed;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, null, "Study completed");
        AddDomainEvent(new StudyCompletedEvent(Id, StudyInstanceUid.Value, InstanceCount, TotalSizeBytes));

        // If results (link/report) already arrived (e.g. ORU before images), advance
        // straight to WaitingForReport / WaitingForImageLinks / Finalized.
        RecomputeCompletion();
    }

    public void MarkQueuedForPacs(string targetPacsId)
    {
        var old = Status;
        TargetPacsId = targetPacsId;
        Status = StudyStatus.QueuedForSend;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, null, $"Queued for PACS {targetPacsId}");
        AddDomainEvent(new StudyStatusChangedEvent(Id, old, Status, $"Queued for PACS {targetPacsId}"));
    }

    public void MarkSendingToPacs()
    {
        var old = Status;
        Status = StudyStatus.Sending;
        PacsSendAttempts++;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, null, $"Sending attempt {PacsSendAttempts}");
        AddDomainEvent(new StudyStatusChangedEvent(Id, old, Status, "Sending to PACS"));
    }

    public void MarkSentToPacs()
    {
        var old = Status;
        Status = StudyStatus.SentToPacs;
        SentToPacsAt = DateTime.UtcNow;
        PacsSendLastError = null;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, null, "Sent to PACS successfully");
        AddDomainEvent(new StudySentToPacsEvent(Id, StudyInstanceUid.Value, TargetPacsId));
    }

    /// <summary>
    /// Manual resend requested from the Hub UI: transitions a <see cref="StudyStatus.Failed"/>
    /// study back into the send pipeline, targeting only the PACS the operator chose.
    /// </summary>
    public void MarkRequeuedManually(IReadOnlyList<string> targetPacsIds)
    {
        var old = Status;
        TargetPacsId = string.Join(",", targetPacsIds);
        Status = StudyStatus.QueuedForSend;
        PacsSendLastError = null;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        var reason = $"Manual resend requested to PACS: {string.Join(", ", targetPacsIds)}";
        RecordStatusChange(old, Status, null, reason);
        AddDomainEvent(new StudyStatusChangedEvent(Id, old, Status, reason));
    }

    public void MarkFailed(string error)
    {
        var old = Status;
        Status = StudyStatus.Failed;
        PacsSendLastError = error;
        RetryCount++;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, Status, null, error);
        AddDomainEvent(new StudyFailedEvent(Id, StudyInstanceUid.Value, error, RetryCount));
    }

    public void RecordWorklistRead(string modalityAeTitle, DateTime timestamp)
    {
        WorklistReadByModality = modalityAeTitle;
        WorklistReadAt = timestamp;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new StudyWorklistReadEvent(Id, StudyInstanceUid.Value, modalityAeTitle));
    }

    public void Release(string? releasedByNodeId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        RecordStatusChange(Status, Status, releasedByNodeId, "Study released");
        AddDomainEvent(new StudyReleasedEvent(Id, StudyInstanceUid.Value, releasedByNodeId));
    }

    public void UpdatePriority(int priority, bool isUrgent = false)
    {
        Priority = priority;
        IsUrgent = isUrgent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMetadata(
        string? studyDescription = null,
        string? referringPhysician = null,
        string? accessionNumber = null,
        int? priority = null,
        bool? isUrgent = null)
    {
        if (studyDescription is not null) StudyDescription = studyDescription.Trim();
        if (referringPhysician is not null) ReferringPhysician = referringPhysician.Trim();
        if (accessionNumber is not null) AccessionNumber = accessionNumber.Trim();
        if (priority.HasValue) Priority = priority.Value;
        if (isUrgent.HasValue) IsUrgent = isUrgent.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Attaches external image/report links received from an ORU^R01 message.
    /// Merges with any previously stored links (deduplicates by URL).
    /// </summary>
    public void AttachImageLinks(IReadOnlyList<string> links)
    {
        if (links.Count == 0) return;

        var existing = string.IsNullOrEmpty(ExternalImageLinks)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                ExternalImageLinks.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

        foreach (var link in links)
        {
            if (!string.IsNullOrWhiteSpace(link))
                existing.Add(link.Trim());
        }

        ExternalImageLinks = string.Join('\n', existing);
        UpdatedAt = DateTime.UtcNow;

        RecomputeCompletion();
    }

    /// <summary>
    /// Attaches a diagnostic report (text/HTML body and/or a PDF stored in the Hub
    /// workspace) received from an ORU^R01 message, then re-derives the study status.
    /// </summary>
    public void AttachReport(ReportFormat format, string? content, string? pdfPath)
    {
        if (format != ReportFormat.None && !string.IsNullOrWhiteSpace(content))
        {
            ReportFormat = format;
            ReportContent = content.Trim();
        }

        if (!string.IsNullOrWhiteSpace(pdfPath))
            ReportPdfPath = pdfPath.Trim();

        if (HasReport)
            ReportReceivedAt ??= DateTime.UtcNow;

        UpdatedAt = DateTime.UtcNow;

        RecomputeCompletion();
    }

    /// <summary>
    /// Re-derives the finalization status from the two deliverable artifacts —
    /// the image link (liga de imágenes) and the diagnostic report:
    /// <list type="bullet">
    ///   <item>liga + reporte → <see cref="StudyStatus.Finalized"/></item>
    ///   <item>solo reporte → <see cref="StudyStatus.WaitingForImageLinks"/></item>
    ///   <item>solo liga → <see cref="StudyStatus.WaitingForReport"/></item>
    /// </list>
    /// No-op once the study has entered the PACS-send pipeline or failed.
    /// </summary>
    public void RecomputeCompletion()
    {
        if (Status is StudyStatus.QueuedForSend or StudyStatus.Sending
                  or StudyStatus.SentToPacs or StudyStatus.Failed)
            return;

        var links = HasImageLinks;
        var report = HasReport;

        StudyStatus next;
        if (links && report) next = StudyStatus.Finalized;
        else if (report)     next = StudyStatus.WaitingForImageLinks;
        else if (links)      next = StudyStatus.WaitingForReport;
        else                 return; // ni liga ni reporte aún

        if (next == Status) return;

        var old = Status;
        Status = next;
        CurrentStatusSince = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RecordStatusChange(old, next, null, $"Completion recomputed → {next}");
        AddDomainEvent(new StudyStatusChangedEvent(Id, old, next, $"Completion recomputed → {next}"));

        if (next == StudyStatus.Finalized)
            AddDomainEvent(new StudyFinalizedEvent(Id, StudyInstanceUid.Value, links, report));
    }

    /// <summary>
    /// Reassigns this study to a different patient (used during HL7 patient merge / ORM MRG processing).
    /// </summary>
    public void ReassignToPatient(string newPatientId, string? newPatientName)
    {
        PatientId = newPatientId;
        if (!string.IsNullOrWhiteSpace(newPatientName))
            PatientName = newPatientName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddSeries(StudySeries series)
    {
        if (!_series.Any(s => s.SeriesInstanceUid == series.SeriesInstanceUid))
        {
            _series.Add(series);
            SeriesCount = _series.Count;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private void RecordStatusChange(StudyStatus? previous, StudyStatus next, string? nodeId, string? reason)
    {
        _statusAudits.Add(StudyStatusAudit.Create(Id, previous, next, nodeId, reason));
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
