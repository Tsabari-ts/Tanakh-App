using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanakh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReadingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reading_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    book = table.Column<string>(type: "text", nullable: false),
                    chapter = table.Column<int>(type: "integer", nullable: false),
                    verse = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reading_progress", x => x.id);
                    table.CheckConstraint("ck_reading_progress_chapter", "chapter >= 1");
                    table.CheckConstraint("ck_reading_progress_section", "section IN ('torah','neviim','ketuvim')");
                    table.CheckConstraint("ck_reading_progress_verse", "verse IS NULL OR verse >= 1");
                    table.ForeignKey(
                        name: "fk_reading_progress_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reading_progress_subscriber_id_section",
                table: "reading_progress",
                columns: new[] { "subscriber_id", "section" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reading_progress");
        }
    }
}
