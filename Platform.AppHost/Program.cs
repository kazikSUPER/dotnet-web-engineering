// ============================================================================
// .NET Aspire AppHost — Оркестрація мікросервісів та бази даних
// ============================================================================

var builder = DistributedApplication.CreateBuilder(args);

// Реєстрація контейнера або екземпляра SQL Server з базою даних BakeryOrdersDB
var sql = builder.AddSqlServer("sql")
                 .AddDatabase("BakeryOrdersDB");

// Реєстрація мікросервісу замовлень пекарні з прив'язкою до бази даних
builder.AddProject("bakery-api", "../Services/Bakery/Bakery.Api/Bakery.Api.csproj")
       .WithReference(sql);

builder.Build().Run();
