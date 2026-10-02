using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

/// <summary>Reintenta SOLO la apertura de la conexión a OSIS (no los comandos). El SQL Server de OSIS se
/// queda por momentos sin hilos de trabajo (esperas THREADPOOL), y entonces rechaza inicios de sesión
/// nuevos con "tiempo de espera en el protocolo de enlace previo al inicio de sesión" (error -2). Ese
/// error no lo cubre EnableRetryOnFailure de EF, y reintentar un comando que agotó su timeout de
/// consulta tampoco sería buena idea (solo carga más al servidor); por eso se usa la lógica de
/// reintentos propia de SqlClient, que aplica únicamente a Open.</summary>
public sealed class OsisConnectionRetryInterceptor : DbConnectionInterceptor
{
    // SqlException.Number cuando se agota el tiempo (al abrir, incluido el protocolo previo al login).
    private const int LoginTimeoutErrorNumber = -2;
    private readonly SqlRetryLogicBaseProvider _openRetry;

    public OsisConnectionRetryInterceptor(ILogger<OsisConnectionRetryInterceptor> logger)
    {
        // 3 intentos en total, espera exponencial desde 1 s y como máximo 10 s entre intentos.
        // SOLO el error -2 (timeout al abrir): el resto de errores transitorios de conexión (64, 233,
        // 10053/10054/10060, ...) ya los reintenta EnableRetryOnFailure de EF. Si esta lista los
        // incluyera también, un mismo corte se reintentaría en ambos niveles (3 x 4 = 12 intentos).
        // Verificado: SqlServerTransientExceptionDetector de EF NO considera transitorio el -2.
        _openRetry = SqlConfigurableRetryFactory.CreateExponentialRetryProvider(new SqlRetryLogicOption
        {
            NumberOfTries = 3,
            DeltaTime = TimeSpan.FromSeconds(1),
            MaxTimeInterval = TimeSpan.FromSeconds(10),
            TransientErrors = new[] { LoginTimeoutErrorNumber }
        });
        _openRetry.Retrying += (_, args) => logger.LogWarning(
            args.Exceptions.LastOrDefault(),
            "No se pudo abrir la conexión a OSIS (intento {Attempt}); se reintenta en {Delay}.",
            args.RetryCount, args.Delay);
    }

    public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
    {
        if (result is SqlConnection sqlConnection) sqlConnection.RetryLogicProvider = _openRetry;
        return result;
    }
}
