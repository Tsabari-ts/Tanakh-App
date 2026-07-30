using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SuppressionList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suppression_list",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_hash = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppression_list", x => x.id);
                    table.CheckConstraint("ck_suppression_list_reason", "reason IN ('hard_bounce','complaint','manual','unsubscribe')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_suppression_list_email_hash",
                table: "suppression_list",
                column: "email_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suppression_list");
        }
    }
}
