namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Serializa las sincronizaciones puntuales (un solo pedido al abrir "Asignar"/"Gestionar")
/// POR NÚMERO DE PEDIDO: dos refrescos del mismo pedido van de a uno (no se pisan al escribir las
/// mismas filas), pero pedidos distintos corren en paralelo. Antes era un único semáforo para toda
/// la API, así que si tres supervisores abrían pedidos distintos a la vez, el tercero esperaba a los
/// otros dos. Sigue SEPARADO de OrderSynchronizationLock: abrir un pedido nunca espera detrás de un
/// sincronización por rango que puede tardar minutos.</summary>
public sealed class OrderOnDemandSyncLock
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Espera el turno de ese pedido; liberar con Dispose (usar con <c>using</c>).</summary>
    public async Task<IDisposable> AcquireAsync(string docNum, CancellationToken cancellationToken)
    {
        string key = docNum.Trim();
        Entry entry;
        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out Entry? existing))
            {
                existing = new Entry();
                _entries[key] = existing;
            }
            existing.References++;
            entry = existing;
        }

        try
        {
            await entry.Gate.WaitAsync(cancellationToken);
        }
        catch
        {
            ReleaseReference(key, entry);
            throw;
        }
        return new Releaser(this, key, entry);
    }

    // Quita la entrada cuando nadie mas la usa, para no acumular un semaforo por cada pedido abierto.
    private void ReleaseReference(string key, Entry entry)
    {
        lock (_sync)
        {
            entry.References--;
            if (entry.References == 0) _entries.Remove(key);
        }
    }

    private sealed class Entry
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public int References { get; set; }
    }

    private sealed class Releaser : IDisposable
    {
        private readonly OrderOnDemandSyncLock _owner;
        private readonly string _key;
        private readonly Entry _entry;
        private bool _disposed;

        public Releaser(OrderOnDemandSyncLock owner, string key, Entry entry)
        {
            _owner = owner; _key = key; _entry = entry;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _entry.Gate.Release();
            _owner.ReleaseReference(_key, _entry);
        }
    }
}
