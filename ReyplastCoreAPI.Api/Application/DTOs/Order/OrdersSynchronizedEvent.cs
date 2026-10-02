namespace ReyplastCoreAPI.Application.DTOs.Order;

/// <summary>Resumen que se emite por SignalR (evento "OrdersSynchronized") cuando un job de
/// sincronización de pedidos termina bien. Los consumidores (MasterLogicApi) lo usan para decidir si
/// recargar su vista: solo interesa si hubo cambios y si caen dentro del rango de fechas que miran.</summary>
public sealed class OrdersSynchronizedEvent
{
    public Guid ExecutionId { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    /// <summary>Tipo de sincronización; actualmente RANGE.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Rango de DocDate solicitado (solo RANGE).</summary>
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    /// <summary>DocDate mínima y máxima entre los pedidos que realmente cambiaron (cabecera o
    /// líneas). Null si no cambió nada.</summary>
    public DateTime? ChangedDateFrom { get; set; }
    public DateTime? ChangedDateTo { get; set; }
    public long RecordsRead { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsChanged { get; set; }
    public int RecordsUnchanged { get; set; }
    public int ChildRecordsCreated { get; set; }
    public int ChildRecordsChanged { get; set; }
    public bool HasChanges { get; set; }
    public DateTime CompletedAt { get; set; }
}
