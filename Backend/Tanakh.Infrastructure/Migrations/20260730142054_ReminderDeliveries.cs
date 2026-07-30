using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReminderDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reminder_deliveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminder_deliveries", x => x.id);
                    table.CheckConstraint("ck_reminder_deliveries_status", "status IN ('pending','sent','failed','skipped')");
                    table.ForeignKey(
                        name: "fk_reminder_deliveries_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reminder_deliveries_idempotency_key",
                table: "reminder_deliveries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reminder_deliveries_status_scheduled_for",
                table: "reminder_deliveries",
                columns: new[] { "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_reminder_deliveries_subscriber_id_scheduled_for",
                table: "reminder_deliveries",
                columns: new[] { "subscriber_id", "scheduled_for" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reminder_deliveries");
        }
    }
}
