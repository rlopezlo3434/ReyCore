using ReyplastCoreAPI.Api.Extensions;
using Hangfire;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApiDocumentation();

WebApplication app = builder.Build();

// Hangfire 1.8+ bloquea el dashboard a peticiones que no vengan de localhost por defecto
// (LocalRequestsOnlyAuthorizationFilter) -- por eso funcionaba local pero no al entrar por
// la IP del servidor. Decision explicita del usuario: dejarlo con el mismo nivel de confianza
// que ya tiene Swagger en esta API (sin autenticacion propia), accesible para toda la red interna.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>(),
    // El dashboard muestra fechas en la hora del navegador (Perú en la red interna); los jobs
    // recurrentes indican además su zona horaria (ver UseOrderSyncSchedules).
    DashboardTitle = "ReyplastCore · Sincronizaciones (hora Perú)"
});
app.UseOrderSyncSchedules();
app.UseApiPipeline();
app.UseApiDocumentation();
app.Run();

public partial class Program
{
}
