using System.Text.Json.Serialization;
using Bakery.Reviews.Application;
using Bakery.Reviews.Infrastructure;
using Bakery.Reviews.Infrastructure.Health;
using Bakery.Reviews.Infrastructure.Initialization;
using Bakery.Reviews.Infrastructure.Seeding;
using Bakery.Reviews.WebApi.Middleware;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Reviews.WebApi"));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

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
