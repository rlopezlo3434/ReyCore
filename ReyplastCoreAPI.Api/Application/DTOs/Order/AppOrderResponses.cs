using System.Text.Json.Serialization;

namespace ReyplastCoreAPI.Application.DTOs.Order;

/// <summary>Filtros del listado de pedidos por aplicación consumidora (GET api/orders/app/{appCode}).
/// AppCode y SourceSystem son obligatorios; el rango de fechas también, salvo que se busque por DocNum.
/// CardCode y CardName son opcionales y comunes a todas las aplicaciones.</summary>
public sealed class AppOrderQuery
{
    public string AppCode { get; set; } = string.Empty;
    public string? SourceSystem { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? DocNum { get; set; }
    public string? CardCode { get; set; }
    public string? CardName { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; }
}

/// <summary>Filtros ya validados que recibe el repositorio. Por DocNum solo se usan SourceSystem y
/// SituationCode; el resto viaja vacío.</summary>
public sealed record AppOrderFilter(
    string SourceSystem,
    string SituationCode,
    string? DocNum,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? CardCode,
    string? CardName);

/// <summary>Página del listado por aplicación. Misma forma que consumía SalesPlace
/// (page/pageSize/totalRecords/totalPages) para que no tenga que remapear.</summary>
public sealed class AppOrderPage<TItem>
{
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>Pedido tal como lo necesita SalesPlace (app "SP"): solo lo que muestra al agendar una cita
/// de recojo. Las unidades se calculan desde ReyplastCoreDB (líneas activas, cantidad menos suspendida),
/// así SalesPlace no tiene que leer detalles ni mapear.</summary>
public sealed class SalesPlaceOrderListItem
{
    public Guid Id { get; set; }
    public string DocNum { get; set; } = string.Empty;
    /// <summary>Código del cliente: RUC o DNI.</summary>
    public string CardCode { get; set; } = string.Empty;
    /// <summary>Nombre o razón social del cliente.</summary>
    public string CardName { get; set; } = string.Empty;
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
    /// <summary>Fecha de aprobación del pedido en el origen; null si aún no está aprobado. Se envía
    /// siempre (también como null) para que el consumidor no tenga que suponer su ausencia.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTime? ApprovalDate { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalUnits { get; set; }
}
