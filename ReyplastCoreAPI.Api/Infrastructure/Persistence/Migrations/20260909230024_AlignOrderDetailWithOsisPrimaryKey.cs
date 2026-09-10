using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class AlignOrderDetailWithOsisPrimaryKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_OrderDetails_Order_LineNumber",
                table: "OrderDetails");

            migrationBuilder.CreateIndex(
                name: "UX_OrderDetails_Order_ItemCode",
                table: "OrderDetails",
                columns: new[] { "OrderId", "ItemCode" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_OrderDetails_Order_ItemCode",
                table: "OrderDetails");

            migrationBuilder.CreateIndex(
                name: "UX_OrderDetails_Order_LineNumber",
                table: "OrderDetails",
                columns: new[] { "OrderId", "LineNumber" },
                unique: true);
        }
    }
}
