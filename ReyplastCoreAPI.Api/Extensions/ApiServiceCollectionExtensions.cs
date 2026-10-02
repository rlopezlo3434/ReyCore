using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using ReyplastCoreAPI.Api.Hubs;
using ReyplastCoreAPI.Application;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Application.Services.Orders;
using ReyplastCoreAPI.Infrastructure;

namespace ReyplastCoreAPI.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        services.AddSingleton<IOrderSyncNotifier, SignalROrderSyncNotifier>();
        services.Configure<OsisApprovedHourlySyncOptions>(
            configuration.GetSection(OsisApprovedHourlySyncOptions.SectionName));
        services.AddApplicationServices(configuration);
        services.AddInfrastructureServices(configuration);
        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            string xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
        });
        return services;
    }
}
