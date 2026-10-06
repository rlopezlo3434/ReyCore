using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Application.Services.Orders;

public static class OrderSyncKinds
{
    public const string Range = "RANGE";
    /// <summary>Job horario: pedidos aprobados del día (hora Perú).</summary>
    public const string ApprovedDay = "APPROVED_DAY";
}

/// <summary>Acumula, a lo largo de las páginas/lotes de una ejecución, el rango de DocDate de los
/// pedidos que realmente cambiaron -- los "touch" (vistos sin cambios) no cuentan.</summary>
public sealed class OrderSyncChangeWindow
{
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }

    public void Include(DateTime docDate)
    {
        DateTime date = docDate.Date;
        if (From is null || date < From) From = date;
        if (To is null || date > To) To = date;
    }

    public OrdersSynchronizedEvent ToEvent(SyncExecution execution, string kind,
        DateTime? dateFrom, DateTime? dateTo) => new()
    {
        ExecutionId = execution.Id,
        SourceSystem = execution.SourceSystem,
        Kind = kind,
        DateFrom = dateFrom?.Date,
        DateTo = dateTo?.Date,
        ChangedDateFrom = From,
        ChangedDateTo = To,
        RecordsRead = execution.RecordsRead,
        RecordsCreated = execution.RecordsCreated,
        RecordsChanged = execution.RecordsChanged,
        RecordsUnchanged = execution.RecordsUnchanged,
        ChildRecordsCreated = execution.ChildRecordsCreated,
        ChildRecordsChanged = execution.ChildRecordsChanged,
        HasChanges = From is not null,
        CompletedAt = execution.CompletedAt ?? DateHelper.PeruNow()
    };
}
