namespace ReyplastCoreAPI.Application.Services.Products;

/// <summary>Evita sincronizaciones simultáneas dentro de una misma instancia de la API.</summary>
public sealed class ProductSynchronizationLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task WaitAsync(CancellationToken cancellationToken) => _semaphore.WaitAsync(cancellationToken);

    public void Release() => _semaphore.Release();
}
