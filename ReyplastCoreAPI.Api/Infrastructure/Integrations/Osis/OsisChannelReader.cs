using System.Data;
using Microsoft.Data.SqlClient;
using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.Interfaces.Integrations;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

internal sealed class OsisChannelReader : IOsisChannelReader
{
    private readonly string _connectionString;

    public OsisChannelReader(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("CloudConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:CloudConnection no está configurado.");

    // [MASTERLOGIC].[ListarCanales] (base CLOUD, mismo servidor que XIMESA) es el catálogo real y
    // cerrado de canales del negocio -- reemplaza al intento anterior de derivarlos desde
    // VENDEDOR_CLIENTE_GRUPOS (una tabla de grupos vendedor-cliente, no un catálogo de canales) o
    // desde una muestra de pedidos recientes, que podía dejar fuera canales sin actividad reciente.
    public async Task<IReadOnlyList<OsisChannelRecord>> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        List<OsisChannelRecord> records = new();
        await using SqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using SqlCommand command = new("[MASTERLOGIC].[ListarCanales]", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        int codeOrdinal = reader.GetOrdinal("codigoCanal");
        int nameOrdinal = reader.GetOrdinal("descripcionCanal");
        while (await reader.ReadAsync(cancellationToken))
        {
            string code = reader.IsDBNull(codeOrdinal) ? string.Empty : reader.GetString(codeOrdinal).Trim();
            string name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetString(nameOrdinal).Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
            records.Add(new OsisChannelRecord { ChannelCode = code, ChannelName = name });
        }

        return records.OrderBy(record => record.ChannelName, StringComparer.Ordinal).ToList();
    }
}
