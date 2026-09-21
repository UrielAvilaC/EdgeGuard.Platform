using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MovePacsReachabilityToNodeAssignment : Migration
    {
        /// <summary>
        /// Mueve la conectividad C-ECHO de <c>pacs_servers</c> (global) a
        /// <c>node_pacs_assignments</c> (par nodo-PACS), que es el nivel en que la pregunta
        /// tiene respuesta: cada nodo sondea desde su propia red contra un AE llamado
        /// concreto, así que dos nodos pueden diferir sobre el mismo PACS y acertar los dos.
        ///
        /// <para><b>Sobre el aviso de posible pérdida de datos:</b> es nominal. Las tres
        /// columnas que se eliminan sólo las escribía <c>PacsServer.UpdateCEchoStatus</c>,
        /// que nunca se llamó desde ningún punto de la solución. Conservan para todas las
        /// filas los valores del alta — <c>false</c>, <c>null</c>, <c>false</c> —, así que
        /// no hay nada que migrar hacia las columnas nuevas.</para>
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_reachable",
                table: "pacs_servers");

            migrationBuilder.DropColumn(
                name: "last_c_echo_at",
                table: "pacs_servers");

            migrationBuilder.DropColumn(
                name: "last_c_echo_success",
                table: "pacs_servers");

            migrationBuilder.AddColumn<string>(
                name: "last_c_echo_error",
                table: "node_pacs_assignments",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_c_echo_error_reason",
                table: "node_pacs_assignments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "last_c_echo_latency_ms",
                table: "node_pacs_assignments",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_c_echo_error",
                table: "node_pacs_assignments");

            migrationBuilder.DropColumn(
                name: "last_c_echo_error_reason",
                table: "node_pacs_assignments");

            migrationBuilder.DropColumn(
                name: "last_c_echo_latency_ms",
                table: "node_pacs_assignments");

            migrationBuilder.AddColumn<bool>(
                name: "is_reachable",
                table: "pacs_servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_c_echo_at",
                table: "pacs_servers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "last_c_echo_success",
                table: "pacs_servers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
