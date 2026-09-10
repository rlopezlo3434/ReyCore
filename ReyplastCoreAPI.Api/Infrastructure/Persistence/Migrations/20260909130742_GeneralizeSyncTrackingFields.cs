using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class GeneralizeSyncTrackingFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_OsisSyncExecutions_LastSeenOsisSyncId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_OsisSyncExecutions_LastSeenOsisSyncId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "LastSeenOsisSyncId",
                table: "Products",
                newName: "SyncExecutionId");

            migrationBuilder.RenameColumn(
                name: "LastOsisChangedAt",
                table: "Products",
                newName: "LastSynchronizedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Products_LastSeenOsisSyncId",
                table: "Products",
                newName: "IX_Products_SyncExecutionId");

            migrationBuilder.RenameColumn(
                name: "LastSeenOsisSyncId",
                table: "Colors",
                newName: "SyncExecutionId");

            migrationBuilder.RenameColumn(
                name: "LastOsisChangedAt",
                table: "Colors",
                newName: "LastSynchronizedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Colors_LastSeenOsisSyncId",
                table: "Colors",
                newName: "IX_Colors_SyncExecutionId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colors_OsisSyncExecutions_SyncExecutionId",
                table: "Colors");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_OsisSyncExecutions_SyncExecutionId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "SyncExecutionId",
                table: "Products",
                newName: "LastSeenOsisSyncId");

            migrationBuilder.RenameColumn(
                name: "LastSynchronizedAt",
                table: "Products",
                newName: "LastOsisChangedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SyncExecutionId",
                table: "Products",
                newName: "IX_Products_LastSeenOsisSyncId");

            migrationBuilder.RenameColumn(
                name: "SyncExecutionId",
                table: "Colors",
                newName: "LastSeenOsisSyncId");

            migrationBuilder.RenameColumn(
                name: "LastSynchronizedAt",
                table: "Colors",
                newName: "LastOsisChangedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Colors_SyncExecutionId",
                table: "Colors",
                newName: "IX_Colors_LastSeenOsisSyncId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colors_OsisSyncExecutions_LastSeenOsisSyncId",
                table: "Colors",
                column: "LastSeenOsisSyncId",
                principalTable: "OsisSyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_OsisSyncExecutions_LastSeenOsisSyncId",
                table: "Products",
                column: "LastSeenOsisSyncId",
                principalTable: "OsisSyncExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
