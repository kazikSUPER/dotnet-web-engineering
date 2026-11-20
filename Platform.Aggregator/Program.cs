using Platform.Aggregator.Clients;
using Platform.Aggregator.Caching;
using Platform.Aggregator.Diagnostics;
using Platform.Aggregator.GrpcClients;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Metrics;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
builder.Services.AddSingleton<AggregatorMetrics>();

builder.Services.AddHealthChecks()
    .AddCheck<DownstreamGrpcHealthCheck>("downstream-grpc", tags: ["ready"])
    .AddRedis(
        builder.Configuration.GetConnectionString("redis") ?? "localhost:6379",
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "ready"],
        timeout: TimeSpan.FromSeconds(3));

var criticalServiceConfig = new ServiceConfig
{
    MethodConfigs =
    {
        new MethodConfig
        {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 5,
                InitialBackoff = TimeSpan.FromSeconds(1),
                MaxBackoff = TimeSpan.FromSeconds(5),
                BackoffMultiplier = 2,
                RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded }
            }
        }
    }
};

var nonCriticalServiceConfig = new ServiceConfig
{
    MethodConfigs =
    {
        new MethodConfig
        {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 2,
                InitialBackoff = TimeSpan.FromMilliseconds(500),
                MaxBackoff = TimeSpan.FromSeconds(2),
                BackoffMultiplier = 2,
                RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded }
            }
        }
    }
};

builder.Services.AddGrpcClient<Orders.Api.Grpc.OrdersService.OrdersServiceClient>(options =>
{
    options.Address = new Uri("http://_grpc.orders-api");
    options.ChannelOptionsActions.Add(opt => opt.ServiceConfig = criticalServiceConfig);
})
.RemoveAllResilienceHandlers()
.AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<Catalog.Api.Grpc.CatalogService.CatalogServiceClient>(options =>
{
    options.Address = new Uri("http://_grpc.catalog-api");
    options.ChannelOptionsActions.Add(opt => opt.ServiceConfig = criticalServiceConfig);
})
.RemoveAllResilienceHandlers()
.AddInterceptor<GrpcClientLoggingInterceptor>();

builder.Services.AddGrpcClient<Reviews.WebApi.Grpc.ReviewsService.ReviewsServiceClient>(options =>
{
    options.Address = new Uri("http://_grpc.reviews-api");
    options.ChannelOptionsActions.Add(opt => opt.ServiceConfig = nonCriticalServiceConfig);
})
.RemoveAllResilienceHandlers()
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
})
.RemoveAllResilienceHandlers()
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 2;
    options.Retry.Delay = TimeSpan.FromMilliseconds(200);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
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
