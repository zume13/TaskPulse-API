using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UrlHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, collation: "C"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLegacy = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDocuments_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskDocuments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskDocuments_CreatedByUserId",
                table: "TaskDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDocuments_TaskId_LegacySlot",
                table: "TaskDocuments",
                column: "TaskId",
                unique: true,
                filter: "\"IsLegacy\"");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDocuments_TaskId_UrlHash",
                table: "TaskDocuments",
                columns: new[] { "TaskId", "UrlHash" },
                unique: true);

            // Preserve the original URL byte-for-byte. Do not validate, rewrite or discard historical links.
            // Task ID is also the stable document ID of the one legacy compatibility slot.
            migrationBuilder.Sql("""
                INSERT INTO "TaskDocuments"
                    ("Id", "TaskId", "Url", "Label", "UrlHash", "CreatedByUserId", "CreatedAtUtc", "UpdatedAtUtc", "Version", "IsLegacy")
                SELECT "Id", "Id", "LinkedDocumentUrl", COALESCE("LinkedDocumentLabel", 'Legacy document'),
                    upper(encode(sha256(convert_to("LinkedDocumentUrl", 'UTF8')), 'hex')),
                    "CreatedBy", "CreatedAtUtc", NULL, "Id", TRUE
                FROM "TaskItems"
                WHERE "LinkedDocumentUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Refuse a rollback that would silently discard additional links or divergent legacy data.
            migrationBuilder.Sql("""
                DO $rollback$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "TaskDocuments" WHERE NOT "IsLegacy")
                       OR EXISTS (
                           SELECT 1 FROM "TaskDocuments" d JOIN "TaskItems" t ON t."Id" = d."TaskId"
                           WHERE d."IsLegacy" AND (d."Url" IS DISTINCT FROM t."LinkedDocumentUrl"
                               OR d."Label" IS DISTINCT FROM COALESCE(t."LinkedDocumentLabel", 'Legacy document')
                               OR d."Version" <> t."Id" OR d."UpdatedAtUtc" IS NOT NULL
                               OR d."CreatedByUserId" <> t."CreatedBy" OR d."CreatedAtUtc" <> t."CreatedAtUtc"))
                    THEN
                        RAISE EXCEPTION 'Rollback would discard task documents. Export and reconcile document data before downgrading.';
                    END IF;
                END $rollback$;
                """);
            migrationBuilder.DropTable(
                name: "TaskDocuments");
        }
    }
}
