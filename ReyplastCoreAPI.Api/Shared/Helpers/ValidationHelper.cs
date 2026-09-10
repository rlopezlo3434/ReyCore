using ReyplastCoreAPI.Shared.Exceptions;

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
        if (pageNumber <= 0) throw new AppException("El número de página debe ser mayor a cero.");
        if (pageSize <= 0)
            throw new AppException("El tamaño de página debe ser mayor a cero.");
    }
}
