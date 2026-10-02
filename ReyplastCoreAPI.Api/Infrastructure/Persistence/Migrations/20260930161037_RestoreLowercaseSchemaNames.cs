using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreLowercaseSchemaNames : Migration
    {
        private static readonly string[] OrderTables =
            ["OrderSituations", "Orders", "OrderDetails", "OrderDetailQuantityHistory"];

        private static readonly string[] CatalogTables =
            ["Products", "Colors", "Channels", "ChannelGroups"];

        private static readonly string[] HangfireTables =
            ["AggregatedCounter", "Counter", "Hash", "Job", "JobParameter", "JobQueue", "List", "Schema", "Server", "Set", "State"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ChangeSchemaCase(migrationBuilder, "Orders", "orders", "__orders_schema_case", OrderTables);
            ChangeSchemaCase(migrationBuilder, "Catalog", "catalog", "__catalog_schema_case", CatalogTables);
            ChangeSchemaCase(migrationBuilder, "HangFire", "hangfire", "__hangfire_schema_case", HangfireTables);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ChangeSchemaCase(migrationBuilder, "orders", "Orders", "__OrdersSchemaCase", OrderTables);
            ChangeSchemaCase(migrationBuilder, "catalog", "Catalog", "__CatalogSchemaCase", CatalogTables);
            ChangeSchemaCase(migrationBuilder, "hangfire", "HangFire", "__HangFireSchemaCase", HangfireTables);
        }

        private static void ChangeSchemaCase(
            MigrationBuilder migrationBuilder,
            string currentSchema,
            string targetSchema,
            string temporarySchema,
            IEnumerable<string> tableNames)
        {
            migrationBuilder.Sql($"CREATE SCHEMA [{temporarySchema}] AUTHORIZATION [dbo];");
            foreach (string tableName in tableNames)
                migrationBuilder.Sql($"ALTER SCHEMA [{temporarySchema}] TRANSFER [{currentSchema}].[{tableName}];");

            migrationBuilder.Sql($"DROP SCHEMA [{currentSchema}];");
            migrationBuilder.Sql($"CREATE SCHEMA [{targetSchema}] AUTHORIZATION [dbo];");

            foreach (string tableName in tableNames)
                migrationBuilder.Sql($"ALTER SCHEMA [{targetSchema}] TRANSFER [{temporarySchema}].[{tableName}];");

            migrationBuilder.Sql($"DROP SCHEMA [{temporarySchema}];");
        }
    }
}
