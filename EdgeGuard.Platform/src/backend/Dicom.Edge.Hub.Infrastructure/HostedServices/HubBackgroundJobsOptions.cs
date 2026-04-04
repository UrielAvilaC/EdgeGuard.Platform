namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class HubBackgroundJobsOptions
{
    public const string SectionName = "HubBackgroundJobs";

    public bool EnableNodeHealthEvaluator { get; set; } = true;
    public int NodeHealthEvaluationIntervalSeconds { get; set; } = 60;

    public bool EnableStudyCleanupEvaluator { get; set; } = true;
    public int StudyCleanupEvaluationIntervalSeconds { get; set; } = 300;
}
