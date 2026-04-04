using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Hub.Domain.Aggregates.Cleanup.Events;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Domain.Aggregates.Cleanup;

/// <summary>
/// Configurable study cleanup policy per modality. Enterprise-level retention management.
/// </summary>
public sealed class StudyCleanupPolicy : AggregateRoot<string>
{
    public ModalityType Modality { get; private set; }
    public int RetentionDays { get; private set; }
    public bool IsEnabled { get; private set; }
    public int Priority { get; private set; }

    /// <summary>
    /// When true, this policy applies to all nodes. Otherwise use SpecificNodeIdsCsv.
    /// </summary>
    public bool ApplyToAllNodes { get; private set; }
    public string? SpecificNodeIdsCsv { get; private set; }

    /// <summary>
    /// UTC time of day when cleanup should execute.
    /// </summary>
    public TimeOnly CleanupTimeUtc { get; private set; }
    public int MaxStudiesPerRun { get; private set; }

    // Execution tracking
    public DateTime? LastExecutedAt { get; private set; }
    public int LastExecutionStudiesDeleted { get; private set; }
    public int LastExecutionErrors { get; private set; }

    private StudyCleanupPolicy() { }

    public static StudyCleanupPolicy Create(
        ModalityType modality,
        int retentionDays,
        TimeOnly? cleanupTimeUtc = null,
        int maxStudiesPerRun = 100,
        bool applyToAllNodes = true,
        int priority = 5)
    {
        if (retentionDays < 1)
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention must be at least 1 day.");

        var policy = new StudyCleanupPolicy
        {
            Id = IdGenerator.NewId(),
            Modality = modality,
            RetentionDays = retentionDays,
            IsEnabled = true,
            Priority = priority,
            ApplyToAllNodes = applyToAllNodes,
            CleanupTimeUtc = cleanupTimeUtc ?? new TimeOnly(2, 0), // default 2:00 AM UTC
            MaxStudiesPerRun = maxStudiesPerRun
        };

        policy.AddDomainEvent(new CleanupPolicyCreatedEvent(policy.Id, modality, retentionDays));
        return policy;
    }

    public void UpdateRetentionDays(int retentionDays)
    {
        if (retentionDays < 1)
            throw new ArgumentOutOfRangeException(nameof(retentionDays));

        RetentionDays = retentionDays;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordExecution(int studiesDeleted, int errors)
    {
        LastExecutedAt = DateTime.UtcNow;
        LastExecutionStudiesDeleted = studiesDeleted;
        LastExecutionErrors = errors;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new CleanupExecutedEvent(Id, Modality, studiesDeleted, errors));
    }

    public void SetSpecificNodes(IEnumerable<string> nodeIds)
    {
        ApplyToAllNodes = false;
        SpecificNodeIdsCsv = string.Join(",", nodeIds);
        UpdatedAt = DateTime.UtcNow;
    }
}
