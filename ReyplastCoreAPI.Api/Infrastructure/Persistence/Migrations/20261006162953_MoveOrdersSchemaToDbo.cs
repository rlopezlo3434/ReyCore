using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveOrdersSchemaToDbo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "OrderSituations",
                schema: "orders",
                newName: "OrderSituations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "orders",
                newName: "Orders",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                schema: "orders",
                newName: "OrderDetails",
                newSchema: "dbo");

            // ALTER SCHEMA TRANSFER solo cambia metadatos (no copia filas); el esquema vacío ya no se usa.
            migrationBuilder.Sql(
                """
                IF SCHEMA_ID(N'orders') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID(N'orders'))
                    EXEC(N'DROP SCHEMA [orders]');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "orders");

            migrationBuilder.RenameTable(
                name: "OrderSituations",
                schema: "dbo",
                newName: "OrderSituations",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "Orders",
                schema: "dbo",
                newName: "Orders",
                newSchema: "orders");

            migrationBuilder.RenameTable(
                name: "OrderDetails",
                schema: "dbo",
                newName: "OrderDetails",
                newSchema: "orders");
        }
    }
}
