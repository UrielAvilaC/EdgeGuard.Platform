namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// snake_case table names for all EF Core entity configurations.
/// Always reference these constants in IEntityTypeConfiguration implementations.
/// </summary>
public static class TableNames
{
    public const string NodeSettings           = "node_settings";
    public const string ModalityConfigurations = "modality_configurations";
    public const string NodeRegistration       = "node_registration";
    public const string DicomPatients          = "dicom_patients";
    public const string DicomStudies           = "dicom_studies";
    public const string DicomSeries            = "dicom_series";
    public const string DicomInstances         = "dicom_instances";
    public const string EdgeQueueItems         = "edge_queue_items";
    public const string StudyTransfers         = "study_transfers";
    public const string TransferErrors         = "transfer_errors";
    public const string AuditLogs              = "audit_logs";
    public const string DicomAssociations      = "dicom_associations";
    public const string StudyArchives          = "study_archives";
    public const string StudyMetrics           = "study_metrics";
    public const string RoutingRules           = "routing_rules";
    public const string WorklistItems          = "worklist_items";
}
