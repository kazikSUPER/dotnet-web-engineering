using Platform.ApiGateway;
using Platform.Auth;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("ApiGateway");

builder.AddServiceDefaults();

builder.Services.AddPlatformAuthentication(builder.Configuration);
builder.Services.AddPlatformAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UsePlatformDefaults("ApiGateway");
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RouteLoggingMiddleware>();
app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();
