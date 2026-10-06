using Microsoft.Data.SqlClient;

namespace ReyplastCoreAPI.Shared.Helpers;

/// <summary>Clasifica errores de SQL Server sin depender del texto del mensaje.</summary>
public static class DatabaseErrorHelper
{
    // 2601: fila duplicada en un índice único; 2627: violación de PRIMARY KEY / UNIQUE.
    private static readonly int[] DuplicateKeyNumbers = { 2601, 2627 };

    /// <summary>true si la excepción (o alguna interna, p. ej. la de DbUpdateException o BulkExtensions)
    /// es una inserción rechazada por llave duplicada.</summary>
    public static bool IsDuplicateKey(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Errors.Cast<SqlError>().Any(error => DuplicateKeyNumbers.Contains(error.Number)))
                return true;
        }
        return false;
    }
}
