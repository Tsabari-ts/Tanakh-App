using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentVersionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "consent_text",
                table: "consent_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "privacy_version",
                table: "consent_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                table: "consent_records",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consent_text",
                table: "consent_records");

            migrationBuilder.DropColumn(
                name: "privacy_version",
                table: "consent_records");

            migrationBuilder.DropColumn(
                name: "terms_version",
                table: "consent_records");
        }
    }
}
