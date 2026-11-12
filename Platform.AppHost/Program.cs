var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql").WithContainerName("dotnet-web-sql");
var catalogDb = sql.AddDatabase("CatalogDb");
var ordersDb = sql.AddDatabase("OrdersDb");

builder.AddProject("catalog-api", "../Services/Catalog/Catalog.Api/Catalog.Api.csproj")
       .WithReference(catalogDb);

builder.AddProject("orders-api", "../Services/Orders/Orders.Api/Orders.Api.csproj")
       .WithReference(ordersDb);

builder.Build().Run();
