using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Application.Services.Products;
using ReyplastCoreAPI.Application.Services.Orders;

namespace ReyplastCoreAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductSyncService, ProductSyncService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderSyncService, OrderSyncService>();
        services.AddSingleton<ProductSynchronizationLock>();
        services.AddSingleton<ProductSyncQueue>();
        services.AddSingleton<IProductSyncQueue>(provider => provider.GetRequiredService<ProductSyncQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<ProductSyncQueue>());
        services.AddSingleton<OrderSyncQueue>();
        services.AddSingleton<IOrderSyncQueue>(provider => provider.GetRequiredService<OrderSyncQueue>());
        services.AddHostedService(provider => provider.GetRequiredService<OrderSyncQueue>());
        return services;
    }
}
