/*
  Backfill reanudable de la primera foto de cantidades por OrderDetail.

  - Ejecutar solamente en RP_ReyPlastCoreBD, despues de aplicar
    20260922181108_AddOrderDetailQuantityHistory.
  - No consulta ni modifica OSIS.
  - Cada lote confirma por separado para evitar una transaccion sobre toda la tabla.
  - NOT EXISTS permite detener y volver a ejecutar sin duplicar la foto inicial.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @LockResult int;
DECLARE @Rows int = 1;
DECLARE @Total bigint = 0;

EXEC @LockResult = sys.sp_getapplock
    @Resource = N'RP_ReyPlastCoreBD:OrderDetailQuantityHistory:InitialBackfill',
    @LockMode = N'Exclusive',
    @LockOwner = N'Session',
    @LockTimeout = 0;

IF @LockResult < 0
    THROW 51000, 'Ya existe un backfill de OrderDetailQuantityHistory en ejecucion.', 1;

WHILE @Rows > 0
BEGIN
    BEGIN TRANSACTION;

    ;WITH MissingDetails AS
    (
        SELECT TOP (5000)
            detail.Id,
            detail.Quantity,
            detail.SuspendedQuantity,
            detail.LastSynchronizedAt,
            detail.SyncExecutionId
        FROM dbo.OrderDetails AS detail WITH (READPAST)
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.OrderDetailQuantityHistory AS history
            WHERE history.OrderDetailId = detail.Id
        )
        ORDER BY detail.Id
    )
    INSERT dbo.OrderDetailQuantityHistory
    (
        OrderDetailId,
        RequestedQuantity,
        SuspendedQuantity,
        AvailableQuantity,
        ObservedAt,
        SyncExecutionId
    )
    SELECT
        detail.Id,
        detail.Quantity,
        detail.SuspendedQuantity,
        CASE
            WHEN detail.Quantity - detail.SuspendedQuantity < 0 THEN 0
            ELSE detail.Quantity - detail.SuspendedQuantity
        END,
        COALESCE(detail.LastSynchronizedAt, SYSUTCDATETIME()),
        detail.SyncExecutionId
    FROM MissingDetails AS detail;

    SET @Rows = @@ROWCOUNT;
    SET @Total += @Rows;

    COMMIT TRANSACTION;

    IF @Rows > 0
        WAITFOR DELAY '00:00:00.100';
END;

EXEC sys.sp_releaseapplock
    @Resource = N'RP_ReyPlastCoreBD:OrderDetailQuantityHistory:InitialBackfill',
    @LockOwner = N'Session';

SELECT @Total AS InsertedRows;
