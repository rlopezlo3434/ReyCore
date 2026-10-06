namespace ReyplastCoreAPI.Shared.Helpers;

/// <summary>
/// Llave de un pedido OSIS en Core: "Sucursal|Pedido" (BranchCode|DocNum). Se usa en el upsert de la
/// sincronización, para agrupar líneas por pedido y como SourceDocumentId. Distinta del cursor de paginación de
/// OSIS, que va "Pedido|Sucursal".
/// </summary>
public static class OrderSourceKey
{
    public static string Build(string? branchCode, string docNum) => $"{branchCode}|{docNum}";

    /// <summary>Número de pedido de una llave "Sucursal|Pedido".</summary>
    public static string DocNum(string key) => key.Split('|', 2)[1];
}
