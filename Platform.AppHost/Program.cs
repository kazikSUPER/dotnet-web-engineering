var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithContainerName("dotnet-web-sql")
    .WithDataVolume("dotnet-web-sql-data");

var catalogDb = sql.AddDatabase("CatalogDb");
var ordersDb = sql.AddDatabase("OrdersDb");

var redis = builder.AddRedis("redis")
    .WithContainerName("dotnet-web-redis")
    .WithDataVolume("dotnet-web-redis-data");

var mongo = builder.AddMongoDB("mongo")
    .WithContainerName("dotnet-web-mongo")
    .WithDataVolume("dotnet-web-mongo-data");

var reviewsDb = mongo.AddDatabase("BakeryReviewsDB");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithHttpEndpoint(port: 5173, targetPort: 5173, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5273, targetPort: 5273, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WaitFor(catalogDb)
    .WaitFor(redis);

var orders = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithReference(redis)
    .WithHttpEndpoint(port: 5172, targetPort: 5172, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5272, targetPort: 5272, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WaitFor(ordersDb)
    .WaitFor(redis);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WithEnvironment("MongoDb__ConnectionString", mongo.Resource.ConnectionStringExpression)
    .WithReference(redis)
    .WithHttpEndpoint(port: 5180, targetPort: 5180, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5280, targetPort: 5280, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WaitFor(mongo)
    .WaitFor(redis);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(reviews)
    .WithReference(catalog.GetEndpoint("grpc"))
    .WithReference(orders.GetEndpoint("grpc"))
    .WithReference(reviews.GetEndpoint("grpc"))
    .WithReference(redis)
    .WithHttpEndpoint(port: 5190, targetPort: 5190, name: "http", isProxied: false)
    .WaitFor(redis)
    .WaitFor(catalog)
    .WaitFor(orders)
    .WaitFor(reviews);

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(reviews)
    .WithReference(aggregator)
    .WithHttpEndpoint(port: 5000, name: "http")
    .WaitFor(catalog)
    .WaitFor(orders)
    .WaitFor(reviews)
    .WaitFor(aggregator)
    .WithExternalHttpEndpoints();

builder.Build().Run();
