using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CapitalizeModuleSchemaNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server normalmente compara identificadores sin distinguir mayúsculas. Para cambiar
            // también el nombre visible del esquema hay que pasar temporalmente los objetos a otro
            // esquema, eliminar el anterior y crearlo otra vez con el casing definitivo.
            migrationBuilder.Sql("CREATE SCHEMA [__OrdersSchemaCase] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [orders].[OrderSituations];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [orders].[Orders];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [orders].[OrderDetails];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [orders].[OrderDetailQuantityHistory];
                DROP SCHEMA [orders];
                """);
            migrationBuilder.Sql("CREATE SCHEMA [Orders] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [Orders] TRANSFER [__OrdersSchemaCase].[OrderSituations];
                ALTER SCHEMA [Orders] TRANSFER [__OrdersSchemaCase].[Orders];
                ALTER SCHEMA [Orders] TRANSFER [__OrdersSchemaCase].[OrderDetails];
                ALTER SCHEMA [Orders] TRANSFER [__OrdersSchemaCase].[OrderDetailQuantityHistory];
                DROP SCHEMA [__OrdersSchemaCase];
                """);

            migrationBuilder.Sql("CREATE SCHEMA [__CatalogSchemaCase] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [catalog].[Products];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [catalog].[Colors];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [catalog].[Channels];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [catalog].[ChannelGroups];
                DROP SCHEMA [catalog];
                """);
            migrationBuilder.Sql("CREATE SCHEMA [Catalog] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [Catalog] TRANSFER [__CatalogSchemaCase].[Products];
                ALTER SCHEMA [Catalog] TRANSFER [__CatalogSchemaCase].[Colors];
                ALTER SCHEMA [Catalog] TRANSFER [__CatalogSchemaCase].[Channels];
                ALTER SCHEMA [Catalog] TRANSFER [__CatalogSchemaCase].[ChannelGroups];
                DROP SCHEMA [__CatalogSchemaCase];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA [__OrdersSchemaCase] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [Orders].[OrderSituations];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [Orders].[Orders];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [Orders].[OrderDetails];
                ALTER SCHEMA [__OrdersSchemaCase] TRANSFER [Orders].[OrderDetailQuantityHistory];
                DROP SCHEMA [Orders];
                """);
            migrationBuilder.Sql("CREATE SCHEMA [orders] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [orders] TRANSFER [__OrdersSchemaCase].[OrderSituations];
                ALTER SCHEMA [orders] TRANSFER [__OrdersSchemaCase].[Orders];
                ALTER SCHEMA [orders] TRANSFER [__OrdersSchemaCase].[OrderDetails];
                ALTER SCHEMA [orders] TRANSFER [__OrdersSchemaCase].[OrderDetailQuantityHistory];
                DROP SCHEMA [__OrdersSchemaCase];
                """);

            migrationBuilder.Sql("CREATE SCHEMA [__CatalogSchemaCase] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [Catalog].[Products];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [Catalog].[Colors];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [Catalog].[Channels];
                ALTER SCHEMA [__CatalogSchemaCase] TRANSFER [Catalog].[ChannelGroups];
                DROP SCHEMA [Catalog];
                """);
            migrationBuilder.Sql("CREATE SCHEMA [catalog] AUTHORIZATION [dbo];");
            migrationBuilder.Sql("""
                ALTER SCHEMA [catalog] TRANSFER [__CatalogSchemaCase].[Products];
                ALTER SCHEMA [catalog] TRANSFER [__CatalogSchemaCase].[Colors];
                ALTER SCHEMA [catalog] TRANSFER [__CatalogSchemaCase].[Channels];
                ALTER SCHEMA [catalog] TRANSFER [__CatalogSchemaCase].[ChannelGroups];
                DROP SCHEMA [__CatalogSchemaCase];
                """);
        }
    }
}
