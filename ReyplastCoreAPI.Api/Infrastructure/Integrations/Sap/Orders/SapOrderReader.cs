using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Integrations.Sap;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders.Dtos;
using ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders.Mappers;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders;

/// <summary>Cliente de solo lectura para el API de pedidos SAP.</summary>
public sealed class SapOrderReader : ISapOrderReader
{
    private readonly HttpClient _httpClient;
    private readonly int _rangeWindowDays;
    public SapOrderReader(HttpClient httpClient, IOptions<SapOrdersApiOptions> options)
    {
        _httpClient = httpClient;
        _rangeWindowDays = options.Value.RangeWindowDays;
    }

    public async Task<IReadOnlyList<SapOrderRecord>> ReadRangeAsync(
        DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default)
    {
        List<SapOrderRecord> result = new();
        DateTime cursor = dateFrom.Date;
        while (cursor <= dateTo.Date)
        {
            DateTime windowEnd = cursor.AddDays(_rangeWindowDays - 1);
            if (windowEnd > dateTo.Date) windowEnd = dateTo.Date;
            string from = DateHelper.IsoDate(cursor);
            string to = DateHelper.IsoDate(windowEnd);
            string requestUri =
                $"api/pedidos/completo?desde={Uri.EscapeDataString(from)}&hasta={Uri.EscapeDataString(to)}";
            List<SapOrderApiDto>? response = await _httpClient.GetFromJsonAsync<List<SapOrderApiDto>>(
                requestUri, cancellationToken);
            if (response is null)
                throw new InvalidDataException("El API SAP devolvió una respuesta vacía.");
            result.AddRange(response.Select(SapOrderMapper.ToRecord));
            cursor = windowEnd.AddDays(1);
        }
        return result;
    }

    public async Task<SapOrderRecord?> ReadByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default)
    {
        string requestUri = $"api/pedidos/{Uri.EscapeDataString(docNum)}/completo";
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        SapOrderApiDto? order = await response.Content.ReadFromJsonAsync<SapOrderApiDto>(
            cancellationToken: cancellationToken);
        if (order is null) throw new InvalidDataException("El API SAP devolvió una respuesta vacía.");
        SapOrderRecord record = SapOrderMapper.ToRecord(order);
        if (!string.Equals(record.DocNum, docNum, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(
                $"SAP devolvió el pedido {record.DocNum} al solicitar {docNum}.");
        return record;
    }
}
