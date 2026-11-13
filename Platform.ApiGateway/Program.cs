using Platform.ApiGateway;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("ApiGateway");

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UsePlatformDefaults("ApiGateway");
app.UseMiddleware<RouteLoggingMiddleware>();
app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();
