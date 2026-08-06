using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriberOtpCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscriber_otp_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    code_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriber_otp_codes", x => x.id);
                    table.CheckConstraint("ck_subscriber_otp_codes_attempts", "attempts <= 3");
                });

            migrationBuilder.CreateIndex(
                name: "ix_subscriber_otp_codes_phone_number_used_expires_at",
                table: "subscriber_otp_codes",
                columns: new[] { "phone_number", "used", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscriber_otp_codes");
        }
    }
}
