using System.Text.Json.Serialization;
using Bakery.Reviews.Application;
using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Diagnostics;
using Bakery.Reviews.Infrastructure;
using Bakery.Reviews.Infrastructure.Health;
using Bakery.Reviews.Infrastructure.Initialization;
using Bakery.Reviews.Infrastructure.Seeding;
using Bakery.Reviews.WebApi.Middleware;
using Microsoft.OpenApi;
using Platform.ServiceDefaults;
using Reviews.WebApi.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Reviews.WebApi");
builder.AddServiceDefaults(ReviewsActivitySources.SourceNames);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<ICacheService, TwoLevelCacheService>();

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());

builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Bakery Reviews API",
    Version = "v1",
    Description = "Сервіс відгуків на MongoDB: Clean Architecture, CQRS через MediatR, behaviors, cursor pagination та optimistic concurrency.",
}));

builder.Services.AddHealthChecks().AddCheck<MongoDbHealthCheck>("mongodb");

var app = builder.Build();

await InitializeDatabaseAsync(app);

app.UsePlatformDefaults("Reviews.WebApi");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapDefaultEndpoints();

try
{
    await app.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();

    try
    {
        await scope.ServiceProvider.GetRequiredService<MongoIndexInitializer>().EnsureIndexesAsync();
        await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        Log.Error(exception, "Не вдалося підготувати MongoDB: сервіс запускається без індексів та початкового наповнення");
    }
}
