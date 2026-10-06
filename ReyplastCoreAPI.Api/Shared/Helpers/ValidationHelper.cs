using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Shared.Helpers;

public static class ValidationHelper
{
    public static void ThrowIfInvalidId(Guid id, string message)
    {
        if (id == Guid.Empty) throw new AppException(message);
    }

    public static void ThrowIfNull(object? value, string message)
    {
        if (value is null) throw new AppException(message);
    }

    public static void ThrowIfNullOrWhiteSpace(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new AppException(message);
    }

    public static void ThrowIfInvalidPage(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) throw new AppException("El número de página (pageNumber) debe ser mayor que cero.");
        if (pageSize <= 0)
            throw new AppException("La cantidad de registros por página (pageSize) debe ser mayor que cero.");
    }
}
