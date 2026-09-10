using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddOsisOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SyncExecutions_SourceSystem_Status_StartedAt",
                table: "SyncExecutions");

            migrationBuilder.RenameColumn(
                name: "LastProductCode",
                table: "SyncExecutions",
                newName: "LastCursor");

            migrationBuilder.AlterColumn<string>(
                name: "LastCursor",
                table: "SyncExecutions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "ProductsUnchanged",
                table: "SyncExecutions",
                newName: "RecordsUnchanged");

            migrationBuilder.RenameColumn(
                name: "ProductsRead",
                table: "SyncExecutions",
                newName: "RecordsRead");

            migrationBuilder.RenameColumn(
                name: "ProductsCreated",
                table: "SyncExecutions",
                newName: "RecordsCreated");

            migrationBuilder.RenameColumn(
                name: "ProductsChanged",
                table: "SyncExecutions",
                newName: "RecordsChanged");

            migrationBuilder.RenameColumn(
                name: "ColorsUnchanged",
                table: "SyncExecutions",
                newName: "ChildRecordsUnchanged");

            migrationBuilder.RenameColumn(
                name: "ColorsCreated",
                table: "SyncExecutions",
                newName: "ChildRecordsCreated");

            migrationBuilder.RenameColumn(
                name: "ColorsChanged",
                table: "SyncExecutions",
                newName: "ChildRecordsChanged");

            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                table: "SyncExecutions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PRODUCTS");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DocNum = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CardName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocStatus = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DocTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ChannelCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ChannelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SyncExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastSynchronizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_SyncExecutions_SyncExecutionId",
                        column: x => x.SyncExecutionId,
                        principalTable: "SyncExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SyncExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastSynchronizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_SyncExecutions_SyncExecutionId",
                        column: x => x.SyncExecutionId,
                        principalTable: "SyncExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncExecutions_Source_Resource_Status_StartedAt",
                table: "SyncExecutions",
                columns: new[] { "SourceSystem", "ResourceType", "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_SyncExecutionId",
                table: "OrderDetails",
                column: "SyncExecutionId");

            migrationBuilder.CreateIndex(
                name: "UX_OrderDetails_Order_ItemCode",
                table: "OrderDetails",
                columns: new[] { "OrderId", "ItemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ChannelCode",
                table: "Orders",
                column: "ChannelCode");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DocDate_DocStatus",
                table: "Orders",
                columns: new[] { "DocDate", "DocStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SyncExecutionId",
                table: "Orders",
                column: "SyncExecutionId");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Source_Branch_DocNum",
                table: "Orders",
                columns: new[] { "SourceSystem", "BranchCode", "DocNum" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_SyncExecutions_Source_Resource_Status_StartedAt",
                table: "SyncExecutions");

            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "SyncExecutions");

            migrationBuilder.RenameColumn(
                name: "RecordsUnchanged",
                table: "SyncExecutions",
                newName: "ProductsUnchanged");

            migrationBuilder.RenameColumn(
                name: "RecordsRead",
                table: "SyncExecutions",
                newName: "ProductsRead");

            migrationBuilder.RenameColumn(
                name: "RecordsCreated",
                table: "SyncExecutions",
                newName: "ProductsCreated");

            migrationBuilder.RenameColumn(
                name: "RecordsChanged",
                table: "SyncExecutions",
                newName: "ProductsChanged");

            migrationBuilder.RenameColumn(
                name: "ChildRecordsUnchanged",
                table: "SyncExecutions",
                newName: "ColorsUnchanged");

            migrationBuilder.RenameColumn(
                name: "ChildRecordsCreated",
                table: "SyncExecutions",
                newName: "ColorsCreated");

            migrationBuilder.RenameColumn(
                name: "ChildRecordsChanged",
                table: "SyncExecutions",
                newName: "ColorsChanged");

            migrationBuilder.AlterColumn<string>(
                name: "LastCursor",
                table: "SyncExecutions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "LastCursor",
                table: "SyncExecutions",
                newName: "LastProductCode");

            migrationBuilder.CreateIndex(
                name: "IX_SyncExecutions_SourceSystem_Status_StartedAt",
                table: "SyncExecutions",
                columns: new[] { "SourceSystem", "Status", "StartedAt" });
        }
    }
}
