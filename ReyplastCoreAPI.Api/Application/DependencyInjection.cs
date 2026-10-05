using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Application.Services.Products;
using ReyplastCoreAPI.Application.Services.Orders;
using ReyplastCoreAPI.Application.Services.Channels;
using ReyplastCoreAPI.Application.DTOs.Integrations.Sap;
using ReyplastCoreAPI.Application.DTOs.Order;

namespace ReyplastCoreAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SapOrderSyncOptions>()
            .Bind(configuration.GetSection(SapOrderSyncOptions.SectionName))
            .Validate(options => options.DatabaseBatchSize > 0,
                "SapOrdersApi:DatabaseBatchSize debe ser mayor que cero.")
            .ValidateOnStart();
        services.AddOptions<AppOrderOptions>()
            .Bind(configuration.GetSection(AppOrderOptions.SectionName));
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductSyncService, ProductSyncService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IAppOrderQueryService, AppOrderQueryService>();
        services.AddScoped<IOsisOrderSyncService, OsisOrderSyncService>();
        services.AddScoped<ISapOrderSyncService, SapOrderSyncService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddSingleton<ProductSynchronizationLock>();
        services.AddScoped<ProductSyncBackgroundJob>();
        services.AddScoped<IProductSyncQueue, ProductSyncQueue>();
        services.AddScoped<OrderSyncBackgroundJob>();
        services.AddScoped<IOrderSyncQueue, OrderSyncQueue>();
        services.AddSingleton<OrderSynchronizationLock>();
        services.AddSingleton<OrderOnDemandSyncLock>();
        return services;
    }
}
