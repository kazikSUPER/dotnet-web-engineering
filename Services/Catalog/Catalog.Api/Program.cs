using Catalog.API.Middleware;
using Catalog.BLL.Interfaces;
using Catalog.BLL.Mapping;
using Catalog.BLL.Services;
using Catalog.DAL.Data;
using Catalog.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Catalog.Api"));

builder.Services.AddDbContext<CatalogDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("CatalogDb"), sql => sql.EnableRetryOnFailure(20, TimeSpan.FromSeconds(5), null))
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
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
