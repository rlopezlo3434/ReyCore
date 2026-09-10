using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class SupportMultiSourceOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderSituations_SituationCode",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderSituations",
                table: "OrderSituations");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SituationCode",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_Source_Branch_DocNum",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_OrderDetails_Order_ItemCode",
                table: "OrderDetails");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "0");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "3");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "4");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "5");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "6");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumn: "Code",
                keyValue: "7");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OrderSituations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1)",
                oldMaxLength: 1);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                table: "OrderSituations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SituationCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1)",
                oldMaxLength: 1);

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Orders",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDetailId",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Conserva la identidad natural de los registros OSIS ya sincronizados.
            migrationBuilder.Sql(
                "UPDATE [Orders] SET [SourceDocumentId] = CONCAT([BranchCode], '|', [DocNum]) " +
                "WHERE [SourceSystem] = 'OSIS' AND [SourceDocumentId] IS NULL;");
            migrationBuilder.Sql(
                "UPDATE [OrderDetails] SET [SourceDetailId] = [ItemCode] " +
                "WHERE [SourceDetailId] = '';");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderSituations",
                table: "OrderSituations",
                columns: new[] { "SourceSystem", "Code" });

            migrationBuilder.InsertData(
                table: "OrderSituations",
                columns: new[] { "Code", "SourceSystem", "Description" },
                values: new object[,]
                {
                    { "0", "OSIS", "PEDIDO ANULADO" },
                    { "1", "OSIS", "PEDIDO POR CALIFICAR" },
                    { "2", "OSIS", "PEDIDO APROBADO" },
                    { "3", "OSIS", "PEDIDO ATENDIDO" },
                    { "4", "OSIS", "PEDIDO RECHAZADO POR CTAS. POR COBRAR" },
                    { "5", "OSIS", "PEDIDO RECHAZADO POR GERENCIA" },
                    { "6", "OSIS", "PEDIDO DESAPROBADO" },
                    { "7", "OSIS", "PEDIDO RETENIDO" },
                    { "C", "SAP", "CERRADO" },
                    { "O", "SAP", "ABIERTO" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SourceSystem_SituationCode",
                table: "Orders",
                columns: new[] { "SourceSystem", "SituationCode" });

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Source_Branch_DocNum",
                table: "Orders",
                columns: new[] { "SourceSystem", "BranchCode", "DocNum" },
                unique: true,
                filter: "[BranchCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Source_SourceDocumentId",
                table: "Orders",
                columns: new[] { "SourceSystem", "SourceDocumentId" },
                unique: true,
                filter: "[SourceDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_OrderDetails_Order_SourceDetailId",
                table: "OrderDetails",
                columns: new[] { "OrderId", "SourceDetailId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderSituations_SourceSystem_SituationCode",
                table: "Orders",
                columns: new[] { "SourceSystem", "SituationCode" },
                principalTable: "OrderSituations",
                principalColumns: new[] { "SourceSystem", "Code" },
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderSituations_SourceSystem_SituationCode",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderSituations",
                table: "OrderSituations");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SourceSystem_SituationCode",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_Source_Branch_DocNum",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_Orders_Source_SourceDocumentId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "UX_OrderDetails_Order_SourceDetailId",
                table: "OrderDetails");

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumns: new[] { "Code", "SourceSystem" },
                keyColumnTypes: new[] { "nvarchar(20)", "nvarchar(20)" },
                keyValues: new object[] { "C", "SAP" });

            migrationBuilder.DeleteData(
                table: "OrderSituations",
                keyColumns: new[] { "Code", "SourceSystem" },
                keyColumnTypes: new[] { "nvarchar(20)", "nvarchar(20)" },
                keyValues: new object[] { "O", "SAP" });

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "OrderSituations");

            migrationBuilder.DropColumn(
                name: "SourceDocumentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SourceDetailId",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OrderSituations",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "SituationCode",
                table: "Orders",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "BranchCode",
                table: "Orders",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderSituations",
                table: "OrderSituations",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SituationCode",
                table: "Orders",
                column: "SituationCode");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_Source_Branch_DocNum",
                table: "Orders",
                columns: new[] { "SourceSystem", "BranchCode", "DocNum" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_OrderDetails_Order_ItemCode",
                table: "OrderDetails",
                columns: new[] { "OrderId", "ItemCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderSituations_SituationCode",
                table: "Orders",
                column: "SituationCode",
                principalTable: "OrderSituations",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
