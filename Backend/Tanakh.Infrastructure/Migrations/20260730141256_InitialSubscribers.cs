using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSubscribers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "subscribers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    preferred_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false, defaultValue: "Asia/Jerusalem"),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unsubscribed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    unsubscribe_reason = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "text", nullable: false, defaultValue: "he-IL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscribers", x => x.id);
                    table.CheckConstraint("ck_subscribers_status", "status IN ('pending_confirmation','active','unsubscribed','bounced','complained')");
                    table.CheckConstraint("ck_subscribers_timezone_not_empty", "length(trim(timezone)) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscribers_email",
                table: "subscribers",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscribers");
        }
    }
}
