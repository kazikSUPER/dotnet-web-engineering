using Catalog.API.Middleware;
using Catalog.Api.Grpc;
using Catalog.BLL.Caching;
using Catalog.BLL.Diagnostics;
using Catalog.BLL.Interfaces;
using Catalog.BLL.Mapping;
using Catalog.BLL.Services;
using Catalog.DAL.Data;
using Catalog.DAL.Interfaces;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UsePlatformSerilog("Catalog.Api");
builder.AddServiceDefaults(CatalogActivitySources.SourceNames);

builder.WebHost.ConfigureKestrel(options => options.ConfigureEndpointDefaults(endpoint =>
    endpoint.Protocols = HttpProtocols.Http1AndHttp2));

builder.Services.AddDbContext<CatalogDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb"), sql => sql.EnableRetryOnFailure(20, TimeSpan.FromSeconds(5), null))
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

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
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await GuardAgainstLegacySchemaAsync(db);
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment()) await DbInitializer.SeedAsync(db);
}

app.UsePlatformDefaults("Catalog.Api");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<CatalogGrpcService>();
app.MapDefaultEndpoints();
await app.RunAsync();

static async Task GuardAgainstLegacySchemaAsync(CatalogDbContext db)
{
    if (!await db.Database.CanConnectAsync()) return;
    var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
    if (appliedMigrations.Any()) return;
    var hasLegacyTables = await db.Database
        .SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = 'Products'")
        .SingleAsync();
    if (hasLegacyTables > 0)
        throw new InvalidOperationException(
            "У базі знайдено схему попередньої лабораторної без застосованих міграцій EF Core. " +
            "Видаліть базу перед запуском: dotnet ef database drop -f --project Services/Catalog/Catalog.Dal --startup-project Services/Catalog/Catalog.Api");
}
