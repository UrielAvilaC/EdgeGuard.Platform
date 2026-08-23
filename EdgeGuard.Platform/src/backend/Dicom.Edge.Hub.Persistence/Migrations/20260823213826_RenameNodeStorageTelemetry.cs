using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameNodeStorageTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "available_storage_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "max_storage_mb",
                table: "nodes");

            migrationBuilder.RenameColumn(
                name: "disk_available_mb",
                table: "health_check_records",
                newName: "storage_volume_free_mb");

            migrationBuilder.AddColumn<DateTime>(
                name: "config_applied_at",
                table: "nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "config_applied_version",
                table: "nodes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_database_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_dicom_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_limit_applied_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_limit_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "storage_measured_at",
                table: "nodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_volume_free_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_volume_total_mb",
                table: "nodes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_database_mb",
                table: "health_check_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_dicom_mb",
                table: "health_check_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "storage_limit_mb",
                table: "health_check_records",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "config_applied_at",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "config_applied_version",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_database_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_dicom_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_limit_applied_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_limit_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_measured_at",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_volume_free_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_volume_total_mb",
                table: "nodes");

            migrationBuilder.DropColumn(
                name: "storage_database_mb",
                table: "health_check_records");

            migrationBuilder.DropColumn(
                name: "storage_dicom_mb",
                table: "health_check_records");

            migrationBuilder.DropColumn(
                name: "storage_limit_mb",
                table: "health_check_records");

            migrationBuilder.RenameColumn(
                name: "storage_volume_free_mb",
                table: "health_check_records",
                newName: "disk_available_mb");

            migrationBuilder.AddColumn<long>(
                name: "available_storage_mb",
                table: "nodes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "max_storage_mb",
                table: "nodes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
