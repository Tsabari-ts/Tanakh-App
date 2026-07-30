using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EmailEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_event_id = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    bounce_type = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_events");
        }
    }
}
