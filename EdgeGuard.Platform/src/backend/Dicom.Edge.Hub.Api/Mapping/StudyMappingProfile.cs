using System;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;

namespace Dicom.Edge.Hub.Api.Mapping;

/// <summary>
/// Mapping profile for <see cref="Study"/> ↔ <see cref="StudyDto"/>.
/// </summary>
public static class StudyMappingProfile
{
    public static StudyDto ToDto(this Study entity) => new()
    {
        Id = entity.Id,
        StudyInstanceUid = entity.StudyInstanceUid.Value,
        AccessionNumber = entity.AccessionNumber,
        StudyDate = entity.StudyDate,
        StudyDescription = entity.StudyDescription,
        ReferringPhysician = entity.ReferringPhysician,
        PatientId = entity.PatientId,
        PatientRecordId = entity.PatientRecordId,
        PatientName = entity.PatientName,
        SourceNodeId = entity.SourceNodeId,
        SourceAeTitle = entity.SourceAeTitle,
        Status = entity.Status.ToString(),
        InstanceCount = entity.InstanceCount,
        // The count the node reported, not Series.Count: per-series detail is not part of
        // the edge contract, so the collection is empty. AddSeries keeps the two in sync
        // if series rows ever exist.
        SeriesCount = entity.SeriesCount,
        TotalSizeBytes = entity.TotalSizeBytes,
        FirstImageReceivedAt = entity.FirstImageReceivedAt,
        LastImageReceivedAt = entity.LastImageReceivedAt,
        Priority = entity.Priority,
        IsUrgent = entity.IsUrgent,
        PacsStatus = entity.PacsStatus.ToString(),
        TargetPacsId = entity.TargetPacsId,
        SentToPacsAt = entity.SentToPacsAt,
        PacsSendAttempts = entity.PacsSendAttempts,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        ReportFormat = entity.ReportFormat.ToString(),
        HasReport = entity.HasReport,
        HasImageLinks = entity.HasImageLinks,
        ImageLinks = string.IsNullOrEmpty(entity.ExternalImageLinks)
            ? []
            : entity.ExternalImageLinks.Split('\n', StringSplitOptions.RemoveEmptyEntries)
    };
}
