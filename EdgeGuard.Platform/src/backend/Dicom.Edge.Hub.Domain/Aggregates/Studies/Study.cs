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

    public void RecordImagesReceived(int count, long sizeBytes, string? modalityAeTitle = null)
    {
        if (count <= 0) return;

        InstanceCount += count;
        TotalSizeBytes += sizeBytes;
        LastImageReceivedAt = DateTime.UtcNow;
        FirstImageReceivedAt ??= DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

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
