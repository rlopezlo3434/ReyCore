using ReyplastCoreAPI.Application.DTOs.Order;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Listado de pedidos por aplicación consumidora (AppCode de PadLock).</summary>
public interface IAppOrderQueryService
{
    /// <summary>Devuelve una <see cref="AppOrderPage{TItem}"/> con la forma de la app pedida (por
    /// ahora solo "SP" = SalesPlace). Lanza AppException 400 si faltan filtros o la app no existe.</summary>
    Task<object> GetAsync(AppOrderQuery query, CancellationToken cancellationToken);
}
