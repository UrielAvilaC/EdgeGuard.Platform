using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Node.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrationNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    event_type = table.Column<int>(type: "INTEGER", nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    source_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    destination_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    action = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    edge_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    patient_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    is_success = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    details = table.Column<string>(type: "TEXT", nullable: true),
                    severity = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    correlation_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dicom_associations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    calling_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    called_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    remote_ip = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    remote_port = table.Column<int>(type: "INTEGER", nullable: false),
                    connected_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    disconnected_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    images_received = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    bytes_received = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    rejection_reason = table.Column<string>(type: "TEXT", nullable: true),
                    edge_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    accepted_contexts = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dicom_associations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dicom_patients",
                columns: table => new
                {
                    patient_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    patient_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    birth_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    sex = table.Column<string>(type: "TEXT", maxLength: 1, nullable: false),
                    patient_age = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    patient_weight_kg = table.Column<double>(type: "REAL", nullable: true),
                    patient_height_m = table.Column<double>(type: "REAL", nullable: true),
                    accession_number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    referring_physician = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    institution_name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    medical_record_number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    allergies = table.Column<string>(type: "TEXT", nullable: true),
                    comments = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dicom_patients", x => x.patient_id);
                });

            migrationBuilder.CreateTable(
                name: "edge_queue_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_attempt_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    next_retry_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    destination = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    instance_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    is_locked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    locked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    locked_by = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    context_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_edge_queue_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modality_configurations",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    port = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 104),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    requires_auth = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    default_destination = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    max_concurrent_conn = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    timeout_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    model_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_connection_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    is_online = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    synced_from_hub_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modality_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "node_registration",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    hub_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    registration_status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    hub_base_url = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    hub_assigned_version = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    registered_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_config_sync_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_registration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "node_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true),
                    value_type = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    is_read_only = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "routing_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    source_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    modality = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    study_desc_contains = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    accession_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    institution_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    min_instance_count = table.Column<int>(type: "INTEGER", nullable: true),
                    max_instance_count = table.Column<int>(type: "INTEGER", nullable: true),
                    custom_condition = table.Column<string>(type: "TEXT", nullable: true),
                    destination_ae = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    send_to_hub = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    send_to_pacs = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    archive_immediately = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    anonymize = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    created_by = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    updated_by = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    match_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    last_matched_at = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_archives",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    archived_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    archive_location = table.Column<string>(type: "TEXT", nullable: false),
                    size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    delete_scheduled_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    reason = table.Column<int>(type: "INTEGER", nullable: false),
                    ArchivedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompressionRatio = table.Column<double>(type: "REAL", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsLegalHold = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_archives", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_metrics",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    total_size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    reception_ms = table.Column<long>(type: "INTEGER", nullable: false),
                    transfer_ms = table.Column<long>(type: "INTEGER", nullable: true),
                    instances_received = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    instances_failed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    avg_image_size = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    first_image_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_image_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_transfers",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    edge_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    started_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    completed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    total_size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    bytes_transferred = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    total_instances = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    instances_xferred = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    instances_failed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    transfer_method = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    source_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    patient_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    study_date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    modality = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_transfers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transfer_errors",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    error_type = table.Column<int>(type: "INTEGER", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", nullable: false),
                    stack_trace = table.Column<string>(type: "TEXT", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    retry_attempt = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    is_resolved = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    resolved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    resolution_notes = table.Column<string>(type: "TEXT", nullable: true),
                    edge_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    context_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_errors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worklist_items",
                columns: table => new
                {
                    accession_number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    patient_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    patient_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    scheduled_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modality = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    procedure_description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true, defaultValue: "pending"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worklist_items", x => x.accession_number);
                });

            migrationBuilder.CreateTable(
                name: "dicom_studies",
                columns: table => new
                {
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    patient_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    patient_name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    study_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    instance_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    last_image_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    received_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    source_ae_title = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    edge_node_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    total_size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    study_description = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    accession_number = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    referring_physician = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    sent_to_hub_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    archived_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", nullable: true),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    correlation_id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dicom_studies", x => x.study_instance_uid);
                    table.ForeignKey(
                        name: "FK_dicom_studies_dicom_patients_patient_id",
                        column: x => x.patient_id,
                        principalTable: "dicom_patients",
                        principalColumn: "patient_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dicom_series",
                columns: table => new
                {
                    series_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    study_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    modality = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    instance_count = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dicom_series", x => x.series_instance_uid);
                    table.ForeignKey(
                        name: "FK_dicom_series_dicom_studies_study_instance_uid",
                        column: x => x.study_instance_uid,
                        principalTable: "dicom_studies",
                        principalColumn: "study_instance_uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dicom_instances",
                columns: table => new
                {
                    sop_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    series_instance_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    sop_class_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    instance_number = table.Column<int>(type: "INTEGER", nullable: false),
                    file_path = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    file_size_bytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    transfer_syntax_uid = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dicom_instances", x => x.sop_instance_uid);
                    table.ForeignKey(
                        name: "FK_dicom_instances_dicom_series_series_instance_uid",
                        column: x => x.series_instance_uid,
                        principalTable: "dicom_series",
                        principalColumn: "series_instance_uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_event_type_time",
                table: "audit_logs",
                columns: new[] { "event_type", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_timestamp",
                table: "audit_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "px_audit_correlation_id",
                table: "audit_logs",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "px_audit_patient_id",
                table: "audit_logs",
                column: "patient_id",
                filter: "\"patient_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "px_audit_study_instance_uid",
                table: "audit_logs",
                column: "study_instance_uid",
                filter: "\"study_instance_uid\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_association_calling_ae_title",
                table: "dicom_associations",
                column: "calling_ae_title");

            migrationBuilder.CreateIndex(
                name: "ix_association_connected_at",
                table: "dicom_associations",
                column: "connected_at");

            migrationBuilder.CreateIndex(
                name: "ix_association_status",
                table: "dicom_associations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_instance_series_instance_uid",
                table: "dicom_instances",
                column: "series_instance_uid");

            migrationBuilder.CreateIndex(
                name: "uq_instance_file_path",
                table: "dicom_instances",
                column: "file_path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_name",
                table: "dicom_patients",
                column: "patient_name");

            migrationBuilder.CreateIndex(
                name: "ix_series_modality",
                table: "dicom_series",
                column: "modality");

            migrationBuilder.CreateIndex(
                name: "ix_series_study_instance_uid",
                table: "dicom_series",
                column: "study_instance_uid");

            migrationBuilder.CreateIndex(
                name: "ix_studies_accession_number",
                table: "dicom_studies",
                column: "accession_number",
                filter: "\"accession_number\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_studies_cleanup",
                table: "dicom_studies",
                columns: new[] { "is_deleted", "status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_studies_patient_id",
                table: "dicom_studies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_studies_source_ae_status",
                table: "dicom_studies",
                columns: new[] { "source_ae_title", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_studies_status_received_at",
                table: "dicom_studies",
                columns: new[] { "status", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_studies_study_date",
                table: "dicom_studies",
                column: "study_date");

            migrationBuilder.CreateIndex(
                name: "px_studies_correlation_id",
                table: "dicom_studies",
                column: "correlation_id",
                filter: "\"correlation_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "px_studies_not_sent",
                table: "dicom_studies",
                column: "status",
                filter: "\"sent_to_hub_at\" IS NULL AND \"is_deleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "ix_queue_dequeue",
                table: "edge_queue_items",
                columns: new[] { "status", "priority", "next_retry_at" });

            migrationBuilder.CreateIndex(
                name: "ix_queue_study_instance_uid",
                table: "edge_queue_items",
                column: "study_instance_uid");

            migrationBuilder.CreateIndex(
                name: "px_queue_stale_locks",
                table: "edge_queue_items",
                columns: new[] { "is_locked", "locked_at" },
                filter: "\"is_locked\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_modality_is_enabled",
                table: "modality_configurations",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "uq_modality_ae_title",
                table: "modality_configurations",
                column: "ae_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_node_settings_category",
                table: "node_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_routing_priority_enabled",
                table: "routing_rules",
                columns: new[] { "priority", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_archive_study_instance_uid",
                table: "study_archives",
                column: "study_instance_uid");

            migrationBuilder.CreateIndex(
                name: "px_archive_pending_delete",
                table: "study_archives",
                column: "delete_scheduled_at",
                filter: "\"is_deleted\" = 0 AND \"delete_scheduled_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_metrics_first_image_at",
                table: "study_metrics",
                column: "first_image_at");

            migrationBuilder.CreateIndex(
                name: "uq_metrics_study_instance_uid",
                table: "study_metrics",
                column: "study_instance_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transfer_status_started_at",
                table: "study_transfers",
                columns: new[] { "status", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_study_instance_uid",
                table: "study_transfers",
                column: "study_instance_uid");

            migrationBuilder.CreateIndex(
                name: "ix_errors_occurred_at",
                table: "transfer_errors",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_errors_study_instance_uid",
                table: "transfer_errors",
                column: "study_instance_uid");

            migrationBuilder.CreateIndex(
                name: "px_errors_unresolved",
                table: "transfer_errors",
                columns: new[] { "error_type", "is_resolved" },
                filter: "\"is_resolved\" = 0");

            migrationBuilder.CreateIndex(
                name: "ix_worklist_patient_id",
                table: "worklist_items",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_worklist_scheduled_date_modality",
                table: "worklist_items",
                columns: new[] { "scheduled_date", "modality" });

            migrationBuilder.CreateIndex(
                name: "ix_worklist_status",
                table: "worklist_items",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "dicom_associations");

            migrationBuilder.DropTable(
                name: "dicom_instances");

            migrationBuilder.DropTable(
                name: "edge_queue_items");

            migrationBuilder.DropTable(
                name: "modality_configurations");

            migrationBuilder.DropTable(
                name: "node_registration");

            migrationBuilder.DropTable(
                name: "node_settings");

            migrationBuilder.DropTable(
                name: "routing_rules");

            migrationBuilder.DropTable(
                name: "study_archives");

            migrationBuilder.DropTable(
                name: "study_metrics");

            migrationBuilder.DropTable(
                name: "study_transfers");

            migrationBuilder.DropTable(
                name: "transfer_errors");

            migrationBuilder.DropTable(
                name: "worklist_items");

            migrationBuilder.DropTable(
                name: "dicom_series");

            migrationBuilder.DropTable(
                name: "dicom_studies");

            migrationBuilder.DropTable(
                name: "dicom_patients");
        }
    }
}
