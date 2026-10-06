using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Paso 1 de 2 del cambio Id bigint -> Guid en orders.OrderDetailQuantityHistory. Se puede aplicar con la
    /// API publicada funcionando:
    /// - Agrega GuidId vacía (instantáneo, solo metadatos) con default NEWSEQUENTIALID(): todo lo que se
    ///   inserte desde ahora, también desde la API anterior, ya trae su Guid.
    /// - Llena las filas existentes por lotes cortos (recorriendo el Id, sin escanear la tabla) con una pausa
    ///   entre lotes y cada lote en su propia transacción: no bloquea la tabla ni infla el log.
    /// No cambia cantidades, fechas ni el Id actual. El paso 2 (ChangeOrderDetailHistoryIdToGuid) mueve la
    /// clave al desplegar. Aplicar con el script (sqlcmd -t 0): el llenado dura más que el timeout de 30 s.
    /// </summary>
    public partial class PrepareOrderDetailHistoryGuidId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'orders.OrderDetailQuantityHistory', N'GuidId') IS NULL
                    ALTER TABLE [orders].[OrderDetailQuantityHistory]
                        ADD [GuidId] uniqueidentifier NULL
                        CONSTRAINT [DF_OrderDetailQuantityHistory_GuidId] DEFAULT NEWSEQUENTIALID();
                """, suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DECLARE @batch bigint = 5000;
                DECLARE @from bigint = (SELECT MIN([Id]) FROM [orders].[OrderDetailQuantityHistory] WHERE [GuidId] IS NULL);
                DECLARE @max bigint = (SELECT MAX([Id]) FROM [orders].[OrderDetailQuantityHistory]);
                WHILE @from IS NOT NULL AND @from <= @max
                BEGIN
                    UPDATE [orders].[OrderDetailQuantityHistory]
                    SET [GuidId] = NEWID()
                    WHERE [Id] >= @from AND [Id] < @from + @batch AND [GuidId] IS NULL;
                    SET @from += @batch;
                    WAITFOR DELAY '00:00:00.100';
                END;
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'orders.OrderDetailQuantityHistory', N'GuidId') IS NOT NULL
                BEGIN
                    ALTER TABLE [orders].[OrderDetailQuantityHistory] DROP CONSTRAINT [DF_OrderDetailQuantityHistory_GuidId];
                    ALTER TABLE [orders].[OrderDetailQuantityHistory] DROP COLUMN [GuidId];
                END;
                """, suppressTransaction: true);
        }
    }
}
