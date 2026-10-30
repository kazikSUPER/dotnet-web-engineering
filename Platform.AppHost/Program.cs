var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .AddDatabase("BakeryOrdersDB");

builder.AddProject("bakery-api", "../Services/Bakery/Bakery.Api/Bakery.Api.csproj")
       .WithReference(sql);

builder.Build().Run();
