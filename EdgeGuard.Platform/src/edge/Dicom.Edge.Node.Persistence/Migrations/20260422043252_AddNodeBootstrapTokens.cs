using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Node.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeBootstrapTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "dicom_patients",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "dicom_patients",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "dicom_patients");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "dicom_patients");
        }
    }
}
