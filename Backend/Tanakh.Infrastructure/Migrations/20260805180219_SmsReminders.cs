using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SmsReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmation_tokens");

            migrationBuilder.DropTable(
                name: "email_events");

            migrationBuilder.DropTable(
                name: "suppression_list");

            migrationBuilder.DropIndex(
                name: "ix_subscribers_email",
                table: "subscribers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscribers_status",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "unsubscribe_reason",
                table: "subscribers");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "subscribers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "message_body",
                table: "reminder_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_response",
                table: "reminder_deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "provider_status_code",
                table: "reminder_deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "segment_count",
                table: "reminder_deliveries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscribers_phone_number",
                table: "subscribers",
                column: "phone_number",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscribers_phone_required_when_active",
                table: "subscribers",
                sql: "status <> 'active' OR phone_number IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscribers_status",
                table: "subscribers",
                sql: "status IN ('active','unsubscribed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_subscribers_phone_number",
                table: "subscribers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscribers_phone_required_when_active",
                table: "subscribers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_subscribers_status",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "subscribers");

            migrationBuilder.DropColumn(
                name: "message_body",
                table: "reminder_deliveries");

            migrationBuilder.DropColumn(
                name: "provider_response",
                table: "reminder_deliveries");

            migrationBuilder.DropColumn(
                name: "provider_status_code",
                table: "reminder_deliveries");

            migrationBuilder.DropColumn(
                name: "segment_count",
                table: "reminder_deliveries");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_at",
                table: "subscribers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "subscribers",
                type: "citext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unsubscribe_reason",
                table: "subscribers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "confirmation_tokens",
                columns: table => new
                {
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "email_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bounce_type = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    event_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_event_id = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_events", x => x.id);
                    table.CheckConstraint("ck_email_events_bounce_type", "bounce_type IS NULL OR bounce_type IN ('hard','soft')");
                    table.CheckConstraint("ck_email_events_event_type", "event_type IN ('delivered','bounce','complaint','open')");
                    table.ForeignKey(
                        name: "fk_email_events_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "suppression_list",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    email_hash = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    source = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppression_list", x => x.id);
                    table.CheckConstraint("ck_suppression_list_reason", "reason IN ('hard_bounce','complaint','manual','unsubscribe')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscribers_email",
                table: "subscribers",
                column: "email",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_subscribers_status",
                table: "subscribers",
                sql: "status IN ('pending_confirmation','active','unsubscribed','bounced','complained')");

            migrationBuilder.CreateIndex(
                name: "ix_confirmation_tokens_subscriber_id",
                table: "confirmation_tokens",
                column: "subscriber_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_events_provider_event_id",
                table: "email_events",
                column: "provider_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_events_provider_message_id",
                table: "email_events",
                column: "provider_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_events_subscriber_id",
                table: "email_events",
                column: "subscriber_id");

            migrationBuilder.CreateIndex(
                name: "ix_suppression_list_email_hash",
                table: "suppression_list",
                column: "email_hash",
                unique: true);
        }
    }
}
