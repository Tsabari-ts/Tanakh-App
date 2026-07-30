using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmationTokensAndDeliveryReaperFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_reminder_deliveries_status",
                table: "reminder_deliveries");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_attempt_at",
                table: "reminder_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_url",
                table: "reminder_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "confirmation_tokens",
                columns: table => new
                {
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_confirmation_tokens", x => x.token_hash);
                    table.CheckConstraint("ck_confirmation_tokens_purpose", "purpose IN ('confirm')");
                    table.ForeignKey(
                        name: "fk_confirmation_tokens_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_reminder_deliveries_status",
                table: "reminder_deliveries",
                sql: "status IN ('pending','sending','sent','failed','skipped')");

            migrationBuilder.CreateIndex(
                name: "ix_confirmation_tokens_subscriber_id",
                table: "confirmation_tokens",
                column: "subscriber_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmation_tokens");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reminder_deliveries_status",
                table: "reminder_deliveries");

            migrationBuilder.DropColumn(
                name: "next_attempt_at",
                table: "reminder_deliveries");

            migrationBuilder.DropColumn(
                name: "target_url",
                table: "reminder_deliveries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reminder_deliveries_status",
                table: "reminder_deliveries",
                sql: "status IN ('pending','sent','failed','skipped')");
        }
    }
}
