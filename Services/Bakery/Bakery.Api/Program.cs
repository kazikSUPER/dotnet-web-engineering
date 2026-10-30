using Bakery.API.Middleware;
using Bakery.BLL.Interfaces;
using Bakery.BLL.Mapping;
using Bakery.BLL.Services;
using Bakery.BLL.Validators;
using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Bakery.Api"));

builder.Services.AddDbContext<BakeryDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("BakeryOrdersDB"))
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BakeryDbContext>();
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

static async Task GuardAgainstLegacySchemaAsync(BakeryDbContext db)
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
            "Видаліть базу перед запуском: dotnet ef database drop -f --project Services/Bakery/Bakery.Dal --startup-project Services/Bakery/Bakery.Api");
}
