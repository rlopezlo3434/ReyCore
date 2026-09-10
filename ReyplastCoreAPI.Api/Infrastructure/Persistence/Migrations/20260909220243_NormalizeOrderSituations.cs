using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class NormalizeOrderSituations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DocStatus",
                table: "Orders",
                newName: "SituationCode");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DocDate_DocStatus",
                table: "Orders",
                newName: "IX_Orders_DocDate_SituationCode");

            migrationBuilder.CreateTable(
                name: "OrderSituations",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSituations", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "OrderSituations",
                columns: new[] { "Code", "Description" },
                values: new object[,]
                {
                    { "0", "PEDIDO ANULADO" },
                    { "1", "PEDIDO POR CALIFICAR" },
                    { "2", "PEDIDO APROBADO" },
                    { "3", "PEDIDO ATENDIDO" },
                    { "4", "PEDIDO RECHAZADO POR CTAS. POR COBRAR" },
                    { "5", "PEDIDO RECHAZADO POR GERENCIA" },
                    { "6", "PEDIDO DESAPROBADO" },
                    { "7", "PEDIDO RETENIDO" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SituationCode",
                table: "Orders",
                column: "SituationCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderSituations_SituationCode",
                table: "Orders",
                column: "SituationCode",
                principalTable: "OrderSituations",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderSituations_SituationCode",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderSituations");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SituationCode",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "SituationCode",
                table: "Orders",
                newName: "DocStatus");

            migrationBuilder.RenameIndex(
                name: "IX_Orders_DocDate_SituationCode",
                table: "Orders",
                newName: "IX_Orders_DocDate_DocStatus");
        }
    }
}
