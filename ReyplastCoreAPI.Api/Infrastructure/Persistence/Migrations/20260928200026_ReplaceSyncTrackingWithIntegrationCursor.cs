using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSyncTrackingWithIntegrationCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_SyncExecutions_SyncExecutionId",
                table: "Channels");

            migrationBuilder.DropForeignKey(
                name: "FK_Colors_SyncExecutions_SyncExecutionId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_SyncExecutions_SyncExecutionId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_SyncExecutions_SyncExecutionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_SyncExecutions_SyncExecutionId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "SyncExecutions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SyncExecutionId",
                table: "Orders");

            migrationBuilder.RenameTable(name: "SyncCheckpoints", newName: "IntegrationCursors");
            migrationBuilder.RenameColumn(name: "SyncType", table: "IntegrationCursors", newName: "CursorType");
            migrationBuilder.RenameColumn(name: "LastSuccessfulWatermark", table: "IntegrationCursors", newName: "CursorUtc");
            migrationBuilder.RenameColumn(name: "UpdatedAt", table: "IntegrationCursors", newName: "UpdatedAtUtc");
            migrationBuilder.RenameIndex(
                name: "UX_SyncCheckpoints_Source_Resource_Type",
                table: "IntegrationCursors",
                newName: "UX_IntegrationCursors_Source_Resource_Type");
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "IntegrationCursors",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RowVersion", table: "IntegrationCursors");
            migrationBuilder.RenameIndex(
                name: "UX_IntegrationCursors_Source_Resource_Type",
                table: "IntegrationCursors",
                newName: "UX_SyncCheckpoints_Source_Resource_Type");
            migrationBuilder.RenameColumn(name: "CursorType", table: "IntegrationCursors", newName: "SyncType");
            migrationBuilder.RenameColumn(name: "CursorUtc", table: "IntegrationCursors", newName: "LastSuccessfulWatermark");
            migrationBuilder.RenameColumn(name: "UpdatedAtUtc", table: "IntegrationCursors", newName: "UpdatedAt");
            migrationBuilder.RenameTable(name: "IntegrationCursors", newName: "SyncCheckpoints");

            migrationBuilder.CreateTable(
                name: "SyncExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BackgroundJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChildRecordsChanged = table.Column<int>(type: "int", nullable: false),
                    ChildRecordsCreated = table.Column<int>(type: "int", nullable: false),
                    ChildRecordsUnchanged = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastCursor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordsChanged = table.Column<int>(type: "int", nullable: false),
                    RecordsCreated = table.Column<int>(type: "int", nullable: false),
                    RecordsRead = table.Column<long>(type: "bigint", nullable: false),
                    RecordsUnchanged = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncExecutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SyncExecutionId",
                table: "Orders",
                column: "SyncExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncExecutions_Source_Resource_Status_StartedAt",
                table: "SyncExecutions",
                columns: new[] { "SourceSystem", "ResourceType", "Status", "StartedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_SyncExecutions_SyncExecutionId",
                table: "Channels",
                column: "SyncExecutionId",
                principalTable: "SyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_SyncExecutions_SyncExecutionId",
                table: "Colors",
                column: "SyncExecutionId",
                principalTable: "SyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_SyncExecutions_SyncExecutionId",
                table: "OrderDetails",
                column: "SyncExecutionId",
                principalTable: "SyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_SyncExecutions_SyncExecutionId",
                table: "Orders",
                column: "SyncExecutionId",
                principalTable: "SyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_SyncExecutions_SyncExecutionId",
                table: "Products",
                column: "SyncExecutionId",
                principalTable: "SyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
