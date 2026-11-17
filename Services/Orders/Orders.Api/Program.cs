using Orders.API.Middleware;
using Orders.Api.Grpc;
using Orders.BLL.Caching;
using Orders.BLL.Diagnostics;
using Orders.BLL.Interfaces;
using Orders.BLL.Mapping;
using Orders.BLL.Services;
using Orders.BLL.Validators;
using Orders.DAL.Data;
using Orders.DAL.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Orders.Api");
builder.AddServiceDefaults(OrdersActivitySources.SourceNames);

builder.Services.AddDbContext<OrdersDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb"), sql => sql.EnableRetryOnFailure(20, TimeSpan.FromSeconds(5), null))
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateCustomerRequestValidator).Assembly);

builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.AddRedisDistributedCache("redis");
builder.Services.AddSingleton<ICacheService, TwoLevelCacheService>();

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await GuardAgainstLegacySchemaAsync(db);
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment()) await DbInitializer.SeedAsync(db);
}

app.UsePlatformDefaults("Orders.Api");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<OrdersGrpcService>();
app.MapDefaultEndpoints();
await app.RunAsync();

static async Task GuardAgainstLegacySchemaAsync(OrdersDbContext db)
{
    if (!await db.Database.CanConnectAsync()) return;
    var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
    if (appliedMigrations.Any()) return;
    var hasLegacyTables = await db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = 'Customers'")
        .SingleAsync();
    if (hasLegacyTables > 0)
        throw new InvalidOperationException(
            "У базі знайдено схему попередньої лабораторної без застосованих міграцій EF Core. " +
            "Видаліть базу перед запуском: dotnet ef database drop -f --project Services/Orders/Orders.Dal --startup-project Services/Orders/Orders.Api");
}
