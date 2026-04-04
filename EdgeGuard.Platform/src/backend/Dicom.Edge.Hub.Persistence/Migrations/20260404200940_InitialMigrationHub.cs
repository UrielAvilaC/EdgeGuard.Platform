using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigrationHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_check_records",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reported_node_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    memory_usage_mb = table.Column<long>(type: "bigint", nullable: true),
                    disk_available_mb = table.Column<long>(type: "bigint", nullable: true),
                    active_associations = table.Column<int>(type: "integer", nullable: true),
                    queued_studies = table.Column<int>(type: "integer", nullable: true),
                    uptime_seconds = table.Column<long>(type: "bigint", nullable: true),
                    latency_ms = table.Column<double>(type: "double precision", nullable: true),
                    bandwidth_mbps = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_check_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hl7_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    message_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trigger_event = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sending_application = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sending_facility = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    message_control_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    client_endpoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    received_on_port = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    patient_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    accession_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    hl7_version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    dispatch_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_node_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    routed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatch_attempts = table.Column<int>(type: "integer", nullable: false),
                    dispatch_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hl7_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hl7_routing_rules",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    match_message_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    match_trigger_event = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    match_sending_facility = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    match_sending_application = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    match_count = table.Column<int>(type: "integer", nullable: false),
                    last_matched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hl7_routing_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hub_audit_logs",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hub_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "node_configuration_profiles",
                columns: table => new
                {
                    node_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    setting_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_overridden = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_configuration_profiles", x => new { x.node_id, x.setting_key });
                });

            migrationBuilder.CreateTable(
                name: "nodes",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    api_endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    location = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    facility_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    health_check_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    max_storage_mb = table.Column<long>(type: "bigint", nullable: false),
                    available_storage_mb = table.Column<long>(type: "bigint", nullable: false),
                    total_studies_received = table.Column<int>(type: "integer", nullable: false),
                    total_studies_sent = table.Column<int>(type: "integer", nullable: false),
                    errors_last24_hours = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacs_send_audits",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pacs_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pacs_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    response_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    response_time_ms = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_send_audits", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacs_servers",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    host_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    max_concurrent_associations = table.Column<int>(type: "integer", nullable: false),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                    last_c_echo_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_c_echo_success = table.Column<bool>(type: "boolean", nullable: false),
                    is_reachable = table.Column<bool>(type: "boolean", nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    supported_modalities_csv = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_servers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patient_dicom_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    patient_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    issuer_of_patient_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    other_patient_ids = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    facility_source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_by_node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_patients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studies",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_instance_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    accession_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    study_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    study_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    referring_physician = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    patient_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    patient_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    source_node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    source_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    receiving_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_status_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    instance_count = table.Column<int>(type: "integer", nullable: false),
                    series_count = table.Column<int>(type: "integer", nullable: false),
                    total_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    first_image_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_image_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    worklist_read_by_modality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    worklist_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    target_pacs_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    sent_to_pacs_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pacs_send_attempts = table.Column<int>(type: "integer", nullable: false),
                    pacs_send_last_error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_urgent = table.Column<bool>(type: "boolean", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_cleanup_policies",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    modality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    apply_to_all_nodes = table.Column<bool>(type: "boolean", nullable: false),
                    specific_node_ids_csv = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    cleanup_time_utc = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    max_studies_per_run = table.Column<int>(type: "integer", nullable: false),
                    last_executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_execution_studies_deleted = table.Column<int>(type: "integer", nullable: false),
                    last_execution_errors = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_cleanup_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    value_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_read_only = table.Column<bool>(type: "boolean", nullable: false),
                    is_encrypted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_notifications",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    patient_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    pacs_viewer_link = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    message_template = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    triggered_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_whatsapp_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pacs_cecho_results",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    health_check_record_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pacs_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pacs_ae_title = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    response_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_cecho_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_cecho_results_health_check_records_health_check_record~",
                        column: x => x.health_check_record_id,
                        principalTable: "health_check_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_pacs_assignments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pacs_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    inherited_from_hub = table.Column<bool>(type: "boolean", nullable: false),
                    last_c_echo_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_c_echo_success = table.Column<bool>(type: "boolean", nullable: true),
                    c_echo_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_node_pacs_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_node_pacs_assignments_nodes_node_id",
                        column: x => x.node_id,
                        principalTable: "nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_series",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    series_instance_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    modality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    series_description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    instance_count = table.Column<int>(type: "integer", nullable: false),
                    series_number = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_series", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_series_studies_study_id",
                        column: x => x.study_id,
                        principalTable: "studies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "study_status_audits",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    study_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_node_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_status_audits", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_status_audits_studies_study_id",
                        column: x => x.study_id,
                        principalTable: "studies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_health_check_records_node_id",
                table: "health_check_records",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_check_records_received_at",
                table: "health_check_records",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ix_hl7_messages_dispatch_queue",
                table: "hl7_messages",
                columns: new[] { "dispatch_status", "priority", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_hl7_messages_dispatch_status",
                table: "hl7_messages",
                column: "dispatch_status");

            migrationBuilder.CreateIndex(
                name: "ix_hl7_messages_message_type",
                table: "hl7_messages",
                column: "message_type");

            migrationBuilder.CreateIndex(
                name: "ix_hl7_messages_received_at",
                table: "hl7_messages",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ix_hl7_messages_status",
                table: "hl7_messages",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_hl7_routing_rules_active",
                table: "hl7_routing_rules",
                columns: new[] { "is_enabled", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_hub_audit_logs_correlation_id",
                table: "hub_audit_logs",
                column: "correlation_id",
                filter: "correlation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_hub_audit_logs_created_at",
                table: "hub_audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_hub_audit_logs_entity_type_entity_id",
                table: "hub_audit_logs",
                columns: new[] { "entity_type", "entity_id" },
                filter: "entity_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_hub_audit_logs_event_type_created_at",
                table: "hub_audit_logs",
                columns: new[] { "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_node_configuration_profiles_node_id",
                table: "node_configuration_profiles",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "ix_node_configuration_profiles_node_id_category",
                table: "node_configuration_profiles",
                columns: new[] { "node_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_node_pacs_assignments_node_id_pacs_id_is_active",
                table: "node_pacs_assignments",
                columns: new[] { "node_id", "pacs_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_nodes_ae_title",
                table: "nodes",
                column: "ae_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nodes_is_deleted",
                table: "nodes",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_is_enabled",
                table: "nodes",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_nodes_status",
                table: "nodes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_cecho_results_checked_at",
                table: "pacs_cecho_results",
                column: "checked_at");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_cecho_results_health_check_record_id",
                table: "pacs_cecho_results",
                column: "health_check_record_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_cecho_results_pacs_id",
                table: "pacs_cecho_results",
                column: "pacs_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_send_audits_pacs_id",
                table: "pacs_send_audits",
                column: "pacs_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_send_audits_sent_at",
                table: "pacs_send_audits",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_send_audits_study_id",
                table: "pacs_send_audits",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_send_audits_study_id_attempt",
                table: "pacs_send_audits",
                columns: new[] { "study_id", "attempt" });

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_ae_title",
                table: "pacs_servers",
                column: "ae_title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_is_enabled",
                table: "pacs_servers",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_servers_is_global",
                table: "pacs_servers",
                column: "is_global");

            migrationBuilder.CreateIndex(
                name: "ix_patients_is_active",
                table: "patients",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_dicom_id",
                table: "patients",
                column: "patient_dicom_id");

            migrationBuilder.CreateIndex(
                name: "ix_patients_patient_name",
                table: "patients",
                column: "patient_name");

            migrationBuilder.CreateIndex(
                name: "ix_studies_is_deleted_created_at",
                table: "studies",
                columns: new[] { "is_deleted", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_studies_patient_id",
                table: "studies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_studies_source_node_id",
                table: "studies",
                column: "source_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_studies_status",
                table: "studies",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_studies_study_date",
                table: "studies",
                column: "study_date");

            migrationBuilder.CreateIndex(
                name: "ix_studies_study_instance_uid",
                table: "studies",
                column: "study_instance_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_cleanup_policies_is_enabled",
                table: "study_cleanup_policies",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "ix_study_cleanup_policies_modality",
                table: "study_cleanup_policies",
                column: "modality");

            migrationBuilder.CreateIndex(
                name: "ix_study_series_series_instance_uid",
                table: "study_series",
                column: "series_instance_uid");

            migrationBuilder.CreateIndex(
                name: "ix_study_series_study_id",
                table: "study_series",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_status_audits_changed_at",
                table: "study_status_audits",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_study_status_audits_study_id",
                table: "study_status_audits",
                column: "study_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_settings_category",
                table: "system_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_created_at",
                table: "whatsapp_notifications",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_status",
                table: "whatsapp_notifications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_status_created_at",
                table: "whatsapp_notifications",
                columns: new[] { "status", "created_at" },
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_whatsapp_notifications_study_id",
                table: "whatsapp_notifications",
                column: "study_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hl7_messages");

            migrationBuilder.DropTable(
                name: "hl7_routing_rules");

            migrationBuilder.DropTable(
                name: "hub_audit_logs");

            migrationBuilder.DropTable(
                name: "node_configuration_profiles");

            migrationBuilder.DropTable(
                name: "node_pacs_assignments");

            migrationBuilder.DropTable(
                name: "pacs_cecho_results");

            migrationBuilder.DropTable(
                name: "pacs_send_audits");

            migrationBuilder.DropTable(
                name: "pacs_servers");

            migrationBuilder.DropTable(
                name: "patients");

            migrationBuilder.DropTable(
                name: "study_cleanup_policies");

            migrationBuilder.DropTable(
                name: "study_series");

            migrationBuilder.DropTable(
                name: "study_status_audits");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "whatsapp_notifications");

            migrationBuilder.DropTable(
                name: "nodes");

            migrationBuilder.DropTable(
                name: "health_check_records");

            migrationBuilder.DropTable(
                name: "studies");
        }
    }
}
