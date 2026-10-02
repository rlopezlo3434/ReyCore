using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeltaAndMoveCatalogToDbo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationCursors");

            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.RenameTable(
                name: "Products",
                schema: "catalog",
                newName: "Products",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Colors",
                schema: "catalog",
                newName: "Colors",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Channels",
                schema: "catalog",
                newName: "Channels",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ChannelGroups",
                schema: "catalog",
                newName: "ChannelGroups",
                newSchema: "dbo");

            migrationBuilder.Sql("""
                IF SCHEMA_ID(N'catalog') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID(N'catalog'))
                    DROP SCHEMA [catalog];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.RenameTable(
                name: "Products",
                schema: "dbo",
                newName: "Products",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "Colors",
                schema: "dbo",
                newName: "Colors",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "Channels",
                schema: "dbo",
                newName: "Channels",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "ChannelGroups",
                schema: "dbo",
                newName: "ChannelGroups",
                newSchema: "catalog");

            migrationBuilder.CreateTable(
                name: "IntegrationCursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CursorType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CursorUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationCursors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_IntegrationCursors_Source_Resource_Type",
                table: "IntegrationCursors",
                columns: new[] { "SourceSystem", "ResourceType", "CursorType" },
                unique: true);
        }
    }
}
