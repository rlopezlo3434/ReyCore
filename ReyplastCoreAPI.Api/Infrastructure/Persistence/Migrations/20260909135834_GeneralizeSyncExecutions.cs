using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>Generaliza el historial OSIS conservando todas las ejecuciones existentes.</summary>
    public partial class GeneralizeSyncExecutions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_OsisSyncExecutions_SyncExecutionId",
                table: "Colors");
            migrationBuilder.DropForeignKey(
                name: "FK_Products_OsisSyncExecutions_SyncExecutionId",
                table: "Products");
            migrationBuilder.DropPrimaryKey(
                name: "PK_OsisSyncExecutions",
                table: "OsisSyncExecutions");
            migrationBuilder.DropIndex(
                name: "IX_OsisSyncExecutions_Status_StartedAt",
                table: "OsisSyncExecutions");

            migrationBuilder.RenameTable(
                name: "OsisSyncExecutions",
                newName: "SyncExecutions");

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "SyncExecutions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OSIS");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncExecutions",
                table: "SyncExecutions",
                column: "Id");
            migrationBuilder.CreateIndex(
                name: "IX_SyncExecutions_SourceSystem_Status_StartedAt",
                table: "SyncExecutions",
                columns: new[] { "SourceSystem", "Status", "StartedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_SyncExecutions_SyncExecutionId",
                table: "Colors",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_SyncExecutions_SyncExecutionId",
                table: "Colors");
            migrationBuilder.DropForeignKey(
                name: "FK_Products_SyncExecutions_SyncExecutionId",
                table: "Products");
            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncExecutions",
                table: "SyncExecutions");
            migrationBuilder.DropIndex(
                name: "IX_SyncExecutions_SourceSystem_Status_StartedAt",
                table: "SyncExecutions");
            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "SyncExecutions");

            migrationBuilder.RenameTable(
                name: "SyncExecutions",
                newName: "OsisSyncExecutions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OsisSyncExecutions",
                table: "OsisSyncExecutions",
                column: "Id");
            migrationBuilder.CreateIndex(
                name: "IX_OsisSyncExecutions_Status_StartedAt",
                table: "OsisSyncExecutions",
                columns: new[] { "Status", "StartedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_OsisSyncExecutions_SyncExecutionId",
                table: "Colors",
                column: "SyncExecutionId",
                principalTable: "OsisSyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(
                name: "FK_Products_OsisSyncExecutions_SyncExecutionId",
                table: "Products",
                column: "SyncExecutionId",
                principalTable: "OsisSyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
