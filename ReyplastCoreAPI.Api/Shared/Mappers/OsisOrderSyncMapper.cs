using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Shared.Mappers;

/// <summary>
/// Centraliza el mapeo OSIS -> modelo local y la comparación usada para decidir si un registro cambió.
/// No consulta ni persiste datos.
/// </summary>
public static class OsisOrderSyncMapper
{
    public static string BuildSourceKey(SalesOrder order) =>
        OrderSourceKey.Build(order.BranchCode, order.DocNum);

    public static string BuildSourceKey(OsisOrderRecord order) =>
        OrderSourceKey.Build(order.BranchCode, order.DocNum);

    public static bool HasHeaderChanged(SalesOrder current, OsisOrderRecord source) =>
        current.DocDate != source.DocDate
        || current.ApprovalDate != source.ApprovalDate
        || current.CardCode != source.CardCode
        || current.CardName != source.CardName
        || current.SituationCode != source.SituationCode
        || current.DocTotal != source.DocTotal
        || current.ChannelCode != source.ChannelCode
        || current.ChannelName != source.ChannelName
        || current.PaymentConditionCode != source.PaymentConditionCode
        || current.PaymentConditionName != source.PaymentConditionName
        || current.IsActive != source.IsActive
        || current.SourceSystem != SourceSystems.Osis
        || (source.LineCount.HasValue && current.SourceLineCount != source.LineCount)
        || (source.TotalUnits.HasValue && current.SourceTotalUnits != source.TotalUnits);

    public static bool HasDetailChanged(SalesOrderDetail current, OsisOrderDetailRecord source) =>
        current.LineNumber != source.LineNumber
        || current.Description != TextHelper.TrimOrNull(source.Description)
        || current.Quantity != source.Quantity
        || current.SuspendedQuantity != source.SuspendedQuantity
        || current.Price != source.Price
        || current.LineTotal != source.LineTotal
        || current.IsActive != source.IsActive;

    public static void ApplyHeader(SalesOrder target, OsisOrderRecord source,
        Guid executionId, DateTime synchronizedAt, bool isNew, bool changed)
    {
        target.SourceDocumentId = BuildSourceKey(source);
        target.BranchCode = source.BranchCode;
        target.DocNum = source.DocNum;
        target.DocDate = source.DocDate;
        target.ApprovalDate = source.ApprovalDate;
        target.CardCode = source.CardCode;
        target.CardName = source.CardName;
        target.SituationCode = source.SituationCode;
        target.DocTotal = source.DocTotal;
        target.ChannelCode = source.ChannelCode;
        target.ChannelName = source.ChannelName;
        target.PaymentConditionCode = source.PaymentConditionCode;
        target.PaymentConditionName = source.PaymentConditionName;
        target.IsActive = source.IsActive;
        target.SourceSystem = SourceSystems.Osis;
        // Solo se pisa si la lectura lo calculó (nunca se borra un resumen ya guardado).
        if (source.LineCount.HasValue) target.SourceLineCount = source.LineCount;
        if (source.TotalUnits.HasValue) target.SourceTotalUnits = source.TotalUnits;
        target.SyncExecutionId = executionId;
        target.LastSynchronizedAt = synchronizedAt;
        if (changed && !isNew) target.ModifiedAt = synchronizedAt;
    }

    public static void ApplyDetail(SalesOrderDetail target, OsisOrderDetailRecord source,
        Guid executionId, DateTime synchronizedAt, bool isNew, bool changed)
    {
        // PRD_CODPRD es parte de la llave real de PPD y es estable dentro de una cabecera.
        target.SourceDetailId = source.ItemCode;
        target.LineNumber = source.LineNumber;
        target.ItemCode = source.ItemCode;
        target.Description = TextHelper.TrimOrNull(source.Description);
        target.Quantity = source.Quantity;
        target.SuspendedQuantity = source.SuspendedQuantity;
        target.Price = source.Price;
        target.LineTotal = source.LineTotal;
        target.IsActive = source.IsActive;
        target.SyncExecutionId = executionId;
        target.LastSynchronizedAt = synchronizedAt;
        if (changed && !isNew) target.ModifiedAt = synchronizedAt;
    }
}
