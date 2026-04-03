using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Studies;

/// <summary>
/// Represents a DICOM series within a study.
/// </summary>
public sealed class StudySeries : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public string SeriesInstanceUid { get; private set; } = default!;
    public string? Modality { get; private set; }
    public string? SeriesDescription { get; private set; }
    public int InstanceCount { get; private set; }
    public int? SeriesNumber { get; private set; }

    private StudySeries() { }

    public static StudySeries Create(
        string studyId,
        string seriesInstanceUid,
        string? modality = null,
        string? seriesDescription = null,
        int? seriesNumber = null)
    {
        return new StudySeries
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId,
            SeriesInstanceUid = seriesInstanceUid,
            Modality = modality?.Trim(),
            SeriesDescription = seriesDescription?.Trim(),
            SeriesNumber = seriesNumber,
            InstanceCount = 0
        };
    }

    public void IncrementInstanceCount(int count = 1)
    {
        InstanceCount += count;
        UpdatedAt = DateTime.UtcNow;
    }
}
