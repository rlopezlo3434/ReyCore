using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Paso 2 de 2: orders.OrderDetailQuantityHistory pasa a tener Id uniqueidentifier como PRIMERA columna
    /// (el GuidId que llenó PrepareOrderDetailHistoryGuidId). SQL Server no reordena columnas, así que la
    /// tabla se reconstruye sin saturar la base:
    /// 1. Tabla nueva con Id primero, su clave, índices y FK (vacía: instantáneo).
    /// 2. Copia por lotes cortos con pausa, cada lote en su propia transacción; la tabla vieja sigue en uso.
    /// 3. Intercambio en una transacción corta: bloquea la vieja, copia lo que llegó durante la copia, la
    ///    elimina y renombra la nueva.
    /// Cantidades, fechas y estado no cambian. Aplicar con el script (sqlcmd -t 0).
    /// </summary>
    public partial class ChangeOrderDetailHistoryIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[orders].[OrderDetailQuantityHistory_New]', N'U') IS NULL
                CREATE TABLE [orders].[OrderDetailQuantityHistory_New] (
                    [Id] uniqueidentifier NOT NULL CONSTRAINT [DF_OrderDetailQuantityHistory_Id] DEFAULT NEWSEQUENTIALID(),
                    [OrderDetailId] uniqueidentifier NOT NULL,
                    [RequestedQuantity] decimal(18,4) NOT NULL,
                    [SuspendedQuantity] decimal(18,4) NOT NULL,
                    [AvailableQuantity] decimal(18,4) NOT NULL,
                    [ObservedAt] datetime2 NOT NULL,
                    [SyncExecutionId] uniqueidentifier NULL,
                    [IsActive] bit NULL,
                    CONSTRAINT [PK_OrderDetailQuantityHistory_New] PRIMARY KEY CLUSTERED ([Id])
                );
                """, suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DECLARE @batch bigint = 5000;
                DECLARE @from bigint = (SELECT MIN([Id]) FROM [orders].[OrderDetailQuantityHistory]);
                DECLARE @max bigint = (SELECT MAX([Id]) FROM [orders].[OrderDetailQuantityHistory]);
                WHILE @from IS NOT NULL AND @from <= @max
                BEGIN
                    INSERT INTO [orders].[OrderDetailQuantityHistory_New]
                        ([Id], [OrderDetailId], [RequestedQuantity], [SuspendedQuantity], [AvailableQuantity],
                         [ObservedAt], [SyncExecutionId], [IsActive])
                    SELECT COALESCE(o.[GuidId], NEWID()), o.[OrderDetailId], o.[RequestedQuantity], o.[SuspendedQuantity],
                           o.[AvailableQuantity], o.[ObservedAt], o.[SyncExecutionId], o.[IsActive]
                    FROM [orders].[OrderDetailQuantityHistory] AS o
                    WHERE o.[Id] >= @from AND o.[Id] < @from + @batch
                      AND NOT EXISTS (SELECT 1 FROM [orders].[OrderDetailQuantityHistory_New] AS n WHERE n.[Id] = o.[GuidId]);
                    SET @from += @batch;
                    WAITFOR DELAY '00:00:00.100';
                END;
                """, suppressTransaction: true);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[orders].[OrderDetailQuantityHistory_New]')
                               AND name = N'IX_OrderDetailQuantityHistory_OrderDetailId_ObservedAt')
                    CREATE INDEX [IX_OrderDetailQuantityHistory_OrderDetailId_ObservedAt]
                        ON [orders].[OrderDetailQuantityHistory_New] ([OrderDetailId], [ObservedAt]) WITH (ONLINE = ON);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[orders].[OrderDetailQuantityHistory_New]')
                               AND name = N'IX_OrderDetailQuantityHistory_SyncExecutionId')
                    CREATE INDEX [IX_OrderDetailQuantityHistory_SyncExecutionId]
                        ON [orders].[OrderDetailQuantityHistory_New] ([SyncExecutionId]) WITH (ONLINE = ON);
                IF OBJECT_ID(N'[orders].[FK_OrderDetailQuantityHistory_New_OrderDetails_OrderDetailId]', N'F') IS NULL
                    ALTER TABLE [orders].[OrderDetailQuantityHistory_New]
                        ADD CONSTRAINT [FK_OrderDetailQuantityHistory_New_OrderDetails_OrderDetailId]
                        FOREIGN KEY ([OrderDetailId]) REFERENCES [orders].[OrderDetails] ([Id]);
                """, suppressTransaction: true);

            migrationBuilder.Sql(
                """
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;
                    -- Nadie escribe en la vieja mientras se copian las últimas filas y se intercambian los nombres.
                    INSERT INTO [orders].[OrderDetailQuantityHistory_New]
                        ([Id], [OrderDetailId], [RequestedQuantity], [SuspendedQuantity], [AvailableQuantity],
                         [ObservedAt], [SyncExecutionId], [IsActive])
                    SELECT COALESCE(o.[GuidId], NEWID()), o.[OrderDetailId], o.[RequestedQuantity], o.[SuspendedQuantity],
                           o.[AvailableQuantity], o.[ObservedAt], o.[SyncExecutionId], o.[IsActive]
                    FROM [orders].[OrderDetailQuantityHistory] AS o WITH (TABLOCKX, HOLDLOCK)
                    WHERE NOT EXISTS (SELECT 1 FROM [orders].[OrderDetailQuantityHistory_New] AS n WHERE n.[Id] = o.[GuidId]);

                    DROP TABLE [orders].[OrderDetailQuantityHistory];
                    EXEC sp_rename N'[orders].[OrderDetailQuantityHistory_New]', N'OrderDetailQuantityHistory';
                    EXEC sp_rename N'[orders].[PK_OrderDetailQuantityHistory_New]', N'PK_OrderDetailQuantityHistory', N'OBJECT';
                    EXEC sp_rename N'[orders].[FK_OrderDetailQuantityHistory_New_OrderDetails_OrderDetailId]',
                        N'FK_OrderDetailQuantityHistory_OrderDetails_OrderDetailId', N'OBJECT';
                COMMIT TRANSACTION;
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                "El Id del historial pasó a uniqueidentifier y no se puede volver a la numeración bigint original. " +
                "Restaurar desde un respaldo si fuera necesario.");
    }
}
