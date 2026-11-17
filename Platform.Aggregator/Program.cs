using Platform.Aggregator.Clients;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Diagnostics;
using Platform.Aggregator.GrpcClients;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Text.Json.Serialization;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Aggregator");

builder.AddServiceDefaults(AggregatorActivitySources.AssemblyName);

builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<IAggregatedCache, AggregatedCache>();
builder.Services.AddSingleton<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<Orders.Api.Grpc.OrdersService.OrdersServiceClient>(options =>
    options.Address = new Uri("http://_grpc.orders-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();
builder.Services.AddGrpcClient<Catalog.Api.Grpc.CatalogService.CatalogServiceClient>(options =>
    options.Address = new Uri("http://_grpc.catalog-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();
builder.Services.AddGrpcClient<Reviews.WebApi.Grpc.ReviewsService.ReviewsServiceClient>(options =>
    options.Address = new Uri("http://_grpc.reviews-api"))
    .AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddHttpClient<OrdersClient>(client =>
{
    client.BaseAddress = new Uri("https+http://orders-api");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https+http://catalog-api");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<ReviewsClient>(client =>
{
    client.BaseAddress = new Uri("https+http://reviews-api");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddTransient<OrderAssemblyService>();
builder.Services.AddTransient<ProductOverviewService>();
builder.Services.AddTransient<GrpcOrderAggregateService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Craft Bakery Aggregator API",
    Version = "v1",
    Description = "Композитні read-only ендпоінти над сервісами Orders, Catalog та Reviews: паралельні виклики, часткові відмови та попередження.",
}));

var app = builder.Build();

app.UsePlatformDefaults("Aggregator");
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
