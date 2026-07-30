using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_hash = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    policy_version = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_records", x => x.id);
                    table.CheckConstraint("ck_consent_records_consent_type", "consent_type IN ('marketing','analytics','functional')");
                    table.ForeignKey(
                        name: "fk_consent_records_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_subscriber_id_consent_type_granted_at",
                table: "consent_records",
                columns: new[] { "subscriber_id", "consent_type", "granted_at" });

            // Append-only, enforced at the DB level (not just by app-layer
            // convention) - revoking consent means inserting a new row with
            // granted = false, never touching an existing one.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION prevent_consent_records_mutation() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'consent_records is append-only: % not allowed', TG_OP;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_consent_records_append_only
                BEFORE UPDATE OR DELETE ON consent_records
                FOR EACH ROW EXECUTE FUNCTION prevent_consent_records_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_consent_records_append_only ON consent_records;
                DROP FUNCTION IF EXISTS prevent_consent_records_mutation();
                """);

            migrationBuilder.DropTable(
                name: "consent_records");
        }
    }
}
