using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSourceLineSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceLineCount",
                schema: "orders",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SourceTotalUnits",
                schema: "orders",
                table: "Orders",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            // Sin relleno del histórico: el resumen se llena solo para los pedidos que se sincronicen desde
            // ahora. La regla vive en OsisLineSummary (y en la consulta agrupada de OsisOrderReader).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceLineCount",
                schema: "orders",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SourceTotalUnits",
                schema: "orders",
                table: "Orders");
        }
    }
}
