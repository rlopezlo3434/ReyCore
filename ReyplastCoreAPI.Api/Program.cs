using ReyplastCoreAPI.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddApiDocumentation();

WebApplication app = builder.Build();

app.UseApiPipeline();
app.UseApiDocumentation();
app.Run();

public partial class Program
{
}
