using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.SqlServer;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;
using ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders;
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
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(10),
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));
        // Hangfire:ServerEnabled=false en un servidor = ese servidor no ejecuta ni programa tareas (solo encola y
        // muestra el dashboard). Con varios servidores sobre la misma base, uno con versión vieja tomaba la tarea
        // y fallaba con "Could not resolve type"; así solo corre donde se decide.
        if (configuration.GetValue("Hangfire:ServerEnabled", true))
        {
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
                options.Queues = new[] { "core-sync", "default" };
            });
        }
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductSyncRepository, ProductSyncRepository>();
        services.AddScoped<ISyncExecutionRepository, SyncExecutionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderSyncRepository, OrderSyncRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();

        string osisConnectionString = configuration.GetConnectionString("OsisConnection") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(osisConnectionString))
            throw new InvalidOperationException("ConnectionStrings:OsisConnection no está configurado.");
        services.AddOptions<OsisOptions>()
            .Bind(configuration.GetSection(OsisOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ActiveStatusCode),
                "Osis:ActiveStatusCode es obligatorio.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.EnabledSaleCode),
                "Osis:EnabledSaleCode es obligatorio.")
            .Validate(options => options.OrderPageSize > 0,
                "Osis:OrderPageSize debe ser mayor que cero.")
            .ValidateOnStart();
        // Reintentos ante fallos transitorios de OSIS: EnableRetryOnFailure cubre los errores
        // transitorios de las consultas (deadlocks, conexión cortada, etc.; no reintenta un timeout de
        // consulta de 120 s) y OsisConnectionRetryInterceptor cubre la apertura de la conexión, que es
        // donde falla cuando el servidor se queda sin hilos (ver comentario en el interceptor).
        // OsisDbContext es de solo lectura y no abre transacciones propias, así que es compatible
        // con la estrategia de reintentos.
        services.AddSingleton<OsisConnectionRetryInterceptor>();
        services.AddDbContext<OsisDbContext>((serviceProvider, options) => options
            .UseSqlServer(osisConnectionString, sql => sql
                .CommandTimeout(120)
                .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null))
            .AddInterceptors(serviceProvider.GetRequiredService<OsisConnectionRetryInterceptor>()));
        services.AddScoped<IOsisCatalogReader, OsisCatalogReader>();
        services.AddScoped<IOsisOrderReader, OsisOrderReader>();

        // [MASTERLOGIC].[ListarCanales] vive en la base CLOUD, en el mismo servidor que XIMESA
        // pero como catálogo aparte (ver OsisChannelReader).
        string cloudConnectionString = configuration.GetConnectionString("CloudConnection") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cloudConnectionString))
            throw new InvalidOperationException("ConnectionStrings:CloudConnection no está configurado.");
        services.AddScoped<IOsisChannelReader, OsisChannelReader>();

        services.AddOptions<SapOrdersApiOptions>()
            .Bind(configuration.GetSection(SapOrdersApiOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "SapOrdersApi:BaseUrl debe ser una URL absoluta.")
            .Validate(options => options.TimeoutSeconds > 0,
                "SapOrdersApi:TimeoutSeconds debe ser mayor que cero.")
            .Validate(options => options.RangeWindowDays > 0,
                "SapOrdersApi:RangeWindowDays debe ser mayor que cero.")
            .ValidateOnStart();
        services.AddHttpClient<ISapOrderReader, SapOrderReader>((provider, client) =>
        {
            SapOrdersApiOptions options = provider.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<SapOrdersApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        return services;
    }
}
