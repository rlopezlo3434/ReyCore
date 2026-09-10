using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Shared.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(SalesOrder order) => new()
    {
        Id = order.Id, DocNum = order.DocNum, DocDate = order.DocDate,
        CardCode = order.CardCode, CardName = order.CardName, SituationCode = order.SituationCode,
        SituationDescription = order.Situation.Description,
        DocTotal = order.DocTotal, ChannelCode = order.ChannelCode, ChannelName = order.ChannelName,
        TotalItems = order.TotalItems,
        IsActive = order.IsActive, SourceSystem = order.SourceSystem,
        SyncExecutionId = order.SyncExecutionId, LastSynchronizedAt = order.LastSynchronizedAt
    };

    public static OrderDetailResponse ToResponse(SalesOrderDetail detail) => new()
    {
        Id = detail.Id, LineNumber = detail.LineNumber, ItemCode = detail.ItemCode,
        Description = detail.Description, Quantity = detail.Quantity, Price = detail.Price,
        LineTotal = detail.LineTotal, IsActive = detail.IsActive,
        SyncExecutionId = detail.SyncExecutionId, LastSynchronizedAt = detail.LastSynchronizedAt
    };

    public static CompleteOrderResponse ToCompleteResponse(SalesOrder order) => new()
    {
        // En una consulta completa la colección ya está cargada y es la fuente del conteo.
        Header = ToResponse(order),
        Details = order.Details.OrderBy(detail => detail.LineNumber).Select(ToResponse).ToList()
    };
}
