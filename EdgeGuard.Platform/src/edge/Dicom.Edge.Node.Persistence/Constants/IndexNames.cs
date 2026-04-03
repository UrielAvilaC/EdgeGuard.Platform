namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// snake_case index names for all database indexes.
/// Suffix convention: ix_ = regular, uq_ = unique, px_ = partial/filtered.
/// </summary>
public static class IndexNames
{
    // ── dicom_studies ─────────────────────────────────────────────────────
    public const string StudiesStatusReceived = "ix_studies_status_received_at";
    public const string StudiesPatient        = "ix_studies_patient_id";
    public const string StudiesSourceStatus   = "ix_studies_source_ae_status";
    public const string StudiesAccession      = "ix_studies_accession_number";
    public const string StudiesDate           = "ix_studies_study_date";
    public const string StudiesNotSent        = "px_studies_not_sent";          // partial
    public const string StudiesCleanup        = "ix_studies_cleanup";
    public const string StudiesCorrelation    = "px_studies_correlation_id";    // partial

    // ── edge_queue_items ──────────────────────────────────────────────────
    public const string QueueDequeue   = "ix_queue_dequeue";                    // HOT PATH
    public const string QueueStudy     = "ix_queue_study_instance_uid";
    public const string QueueStaleLock = "px_queue_stale_locks";                // partial

    // ── audit_logs ────────────────────────────────────────────────────────
    public const string AuditTimestamp   = "ix_audit_timestamp";
    public const string AuditEventTime   = "ix_audit_event_type_time";
    public const string AuditStudy       = "px_audit_study_instance_uid";       // partial
    public const string AuditCorrelation = "px_audit_correlation_id";           // partial
    public const string AuditPatient     = "px_audit_patient_id";               // partial

    // ── transfer_errors ───────────────────────────────────────────────────
    public const string ErrorsUnresolved = "px_errors_unresolved";              // partial
    public const string ErrorsStudy      = "ix_errors_study_instance_uid";
    public const string ErrorsOccurred   = "ix_errors_occurred_at";

    // ── study_archives ────────────────────────────────────────────────────
    public const string ArchivePending = "px_archive_pending_delete";           // partial
    public const string ArchiveStudy   = "ix_archive_study_instance_uid";

    // ── modality_configurations ───────────────────────────────────────────
    public const string ModalityAeTitle   = "uq_modality_ae_title";
    public const string ModalityEnabled   = "ix_modality_is_enabled";

    // ── dicom_instances ───────────────────────────────────────────────────
    public const string InstanceFilePath = "uq_instance_file_path";
    public const string InstanceSeries   = "ix_instance_series_instance_uid";

    // ── dicom_series ──────────────────────────────────────────────────────
    public const string SeriesStudy    = "ix_series_study_instance_uid";
    public const string SeriesModality = "ix_series_modality";

    // ── dicom_associations ────────────────────────────────────────────────
    public const string AssociationCallingAe  = "ix_association_calling_ae_title";
    public const string AssociationConnected  = "ix_association_connected_at";
    public const string AssociationStatus     = "ix_association_status";

    // ── study_metrics ─────────────────────────────────────────────────────
    public const string MetricsStudy = "uq_metrics_study_instance_uid";

    // ── routing_rules ─────────────────────────────────────────────────────
    public const string RoutingPriority = "ix_routing_priority_enabled";

    // ── worklist_items ────────────────────────────────────────────────────
    public const string WorklistScheduled = "ix_worklist_scheduled_date_modality";
    public const string WorklistPatient   = "ix_worklist_patient_id";
    public const string WorklistStatus    = "ix_worklist_status";

    // ── study_transfers ───────────────────────────────────────────────────
    public const string TransferStudy  = "ix_transfer_study_instance_uid";
    public const string TransferStatus = "ix_transfer_status_started_at";
}
