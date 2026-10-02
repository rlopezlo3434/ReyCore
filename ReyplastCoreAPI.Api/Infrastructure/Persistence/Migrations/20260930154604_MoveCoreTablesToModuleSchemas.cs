using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCoreTablesToModuleSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Products",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "OrderSituations",
                newName: "OrderSituations",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Orders",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                newName: "OrderDetails",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "OrderDetailQuantityHistory",
                newName: "OrderDetailQuantityHistory",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "Colors",
                newName: "Colors",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "Channels",
                newName: "Channels",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "ChannelGroups",
                newName: "ChannelGroups",
                newSchema: "catalog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Products",
                schema: "catalog",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "OrderSituations",
                schema: "orders",
                newName: "OrderSituations");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "orders",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                schema: "orders",
                newName: "OrderDetails");

            migrationBuilder.RenameTable(
                name: "OrderDetailQuantityHistory",
                schema: "orders",
                newName: "OrderDetailQuantityHistory");

            migrationBuilder.RenameTable(
                name: "Colors",
                schema: "catalog",
                newName: "Colors");

            migrationBuilder.RenameTable(
                name: "Channels",
                schema: "catalog",
                newName: "Channels");

            migrationBuilder.RenameTable(
                name: "ChannelGroups",
                schema: "catalog",
                newName: "ChannelGroups");
        }
    }
}
