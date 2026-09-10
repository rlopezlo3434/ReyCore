namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Cola en memoria que desacopla la sincronización de la conexión HTTP.</summary>
public interface IProductSyncQueue
{
    /// <summary>Registra una ejecución OSIS para que el worker la procese una sola vez.</summary>
    bool TryEnqueueOsis(Guid executionId);

    /// <summary>Solicita la cancelación de una ejecución en cola o en curso.</summary>
    bool TryCancel(Guid executionId);
}
