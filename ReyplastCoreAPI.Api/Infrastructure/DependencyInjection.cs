using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;
using ReyplastCoreAPI.Infrastructure.Persistence;
using ReyplastCoreAPI.Infrastructure.Repositories;

namespace ReyplastCoreAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("SqlServerConnection") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:SqlServerConnection no está configurado.");
        services.AddDbContext<ReyplastCoreDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductSyncRepository, ProductSyncRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderSyncRepository, OrderSyncRepository>();

        string osisConnectionString = configuration.GetConnectionString("OsisConnection") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(osisConnectionString))
            throw new InvalidOperationException("ConnectionStrings:OsisConnection no está configurado.");
        services.AddOptions<OsisOptions>()
            .Bind(configuration.GetSection(OsisOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ActiveStatusCode),
                "Osis:ActiveStatusCode es obligatorio.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.EnabledSaleCode),
                "Osis:EnabledSaleCode es obligatorio.")
            .ValidateOnStart();
        services.AddDbContext<OsisDbContext>(options =>
            options.UseSqlServer(osisConnectionString, sql => sql.CommandTimeout(120)));
        services.AddScoped<IOsisCatalogReader, OsisCatalogReader>();
        services.AddScoped<IOsisOrderReader, OsisOrderReader>();
        return services;
    }
}
