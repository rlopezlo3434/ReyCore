using System.Globalization;
using ReyplastCoreAPI.Application.DTOs.Integrations.Sap;
using ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders.Dtos;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders.Mappers;

/// <summary>Traduce únicamente el contrato HTTP SAP al modelo normalizado de aplicación.</summary>
public static class SapOrderMapper
{
    public static SapOrderRecord ToRecord(SapOrderApiDto source)
    {
        string status = Required(source.DocStatus, "DocStatus").ToUpperInvariant();
        if (status is not ("O" or "C"))
            throw new InvalidDataException($"SAP devolvió el DocStatus no soportado '{status}'.");

        List<SapOrderDetailApiDto> sourceDetails = source.Details ?? new List<SapOrderDetailApiDto>();
        if (source.TotalItems != sourceDetails.Count)
            throw new InvalidDataException(
                $"SAP informó {source.TotalItems} detalles para DocEntry {source.DocEntry}, " +
                $"pero devolvió {sourceDetails.Count}.");
        if (sourceDetails.Any(detail => detail.DocEntry != source.DocEntry || detail.DocNum != source.DocNum))
            throw new InvalidDataException(
                $"SAP devolvió una línea que no pertenece al DocEntry {source.DocEntry}.");
        IReadOnlyList<SapOrderDetailRecord> details = sourceDetails.Select(ToRecord).ToList();

        return new SapOrderRecord
        {
            SourceDocumentId = source.DocEntry.ToString(CultureInfo.InvariantCulture),
            DocNum = source.DocNum.ToString(CultureInfo.InvariantCulture),
            DocDate = source.DocDate,
            CardCode = Required(source.CardCode, "CardCode"),
            CardName = Required(source.CardName, "CardName"),
            SituationCode = status,
            DocTotal = source.DocTotal,
            ChannelCode = TextHelper.TrimOrNull(source.ChannelCode),
            ChannelName = TextHelper.TrimOrNull(source.ChannelName),
            Details = details
        };
    }

    private static SapOrderDetailRecord ToRecord(SapOrderDetailApiDto source) => new()
    {
        ItemCode = Required(source.ItemCode, "Detalle.ItemCode"),
        Description = TextHelper.TrimOrNull(source.Description),
        Quantity = source.Quantity,
        Price = source.Price,
        LineTotal = source.LineTotal
    };

    private static string Required(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidDataException($"SAP no devolvió el campo obligatorio {field}.")
            : value.Trim();
}
