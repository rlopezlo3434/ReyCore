using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropOrderDetailQuantityHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderDetailQuantityHistory",
                schema: "orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderDetailQuantityHistory",
                schema: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    OrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SuspendedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SyncExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetailQuantityHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetailQuantityHistory_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalSchema: "orders",
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailQuantityHistory_OrderDetailId_ObservedAt",
                schema: "orders",
                table: "OrderDetailQuantityHistory",
                columns: new[] { "OrderDetailId", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailQuantityHistory_SyncExecutionId",
                schema: "orders",
                table: "OrderDetailQuantityHistory",
                column: "SyncExecutionId");
        }
    }
}
