var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("sql", sqlPassword)
    .WithContainerName("dotnet-web-sql")
    .WithDataVolume("dotnet-web-sql-data");

var catalogDb = sql.AddDatabase("CatalogDb");
var ordersDb = sql.AddDatabase("OrdersDb");
var identityDb = sql.AddDatabase("IdentityDb");
var identityServerDb = sql.AddDatabase("IdentityServerDb");

var redis = builder.AddRedis("redis")
    .WithContainerName("dotnet-web-redis")
    .WithDataVolume("dotnet-web-redis-data")
    .WithPersistence();

var mongo = builder.AddMongoDB("mongo")
    .WithContainerName("dotnet-web-mongo")
    .WithDataVolume("dotnet-web-mongo-data");

var reviewsDb = mongo.AddDatabase("BakeryReviewsDB");

var messaging = builder.AddRabbitMQ("messaging")
    .WithContainerName("dotnet-web-rabbitmq")
    .WithDataVolume("dotnet-web-rabbitmq-data")
    .WithManagementPlugin();

var keycloakDb = sql.AddDatabase("KeycloakDb");

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.0")
    .WithContainerName("dotnet-web-keycloak")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithArgs("start-dev");

var auth = builder.AddProject<Projects.Auth_Api>("auth-api")
    .WithReference(identityDb)
    .WithHttpEndpoint(port: 5300, targetPort: 5300, name: "http", isProxied: false)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(identityDb);

var identityServer = builder.AddProject<Projects.Platform_IdentityServer>("identityserver")
    .WithReference(identityServerDb)
    .WithHttpEndpoint(port: 5400, targetPort: 5400, name: "http", isProxied: false)
    .WaitFor(identityServerDb);

var payments = builder.AddProject<Projects.Payments_Api>("payments-api")
    .WithHttpEndpoint(port: 5185, targetPort: 5185, name: "http", isProxied: false);

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5173, targetPort: 5173, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5273, targetPort: 5273, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(catalogDb)
    .WaitFor(redis)
    .WaitFor(messaging);

var orders = builder.AddProject<Projects.Orders_Api>("orders-api")
    .WithReference(ordersDb)
    .WithReference(redis)
    .WithReference(messaging)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5172, targetPort: 5172, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5272, targetPort: 5272, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(ordersDb)
    .WaitFor(redis)
    .WaitFor(messaging);

var reviews = builder.AddProject<Projects.Reviews_WebApi>("reviews-api")
    .WithReference(reviewsDb)
    .WithEnvironment("MongoDb__ConnectionString", mongo.Resource.ConnectionStringExpression)
    .WithReference(redis)
    .WithReference(messaging)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5180, targetPort: 5180, name: "http", isProxied: false)
    .WithHttpEndpoint(port: 5280, targetPort: 5280, name: "grpc", isProxied: false)
    .WithEndpoint("grpc", endpoint => endpoint.ExcludeReferenceEndpoint = true)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(mongo)
    .WaitFor(redis)
    .WaitFor(messaging);

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(reviews)
    .WithReference(catalog.GetEndpoint("grpc"))
    .WithReference(orders.GetEndpoint("grpc"))
    .WithReference(reviews.GetEndpoint("grpc"))
    .WithReference(redis)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5190, targetPort: 5190, name: "http", isProxied: false)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(redis)
    .WaitFor(catalog)
    .WaitFor(orders)
    .WaitFor(reviews);

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithReference(catalog)
    .WithReference(orders)
    .WithReference(reviews)
    .WithReference(aggregator)
    .WithReference(auth)
    .WithReference(identityServer)
    .WithReference(payments)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithHttpEndpoint(port: 5000, targetPort: 5000, name: "http", isProxied: false)
    .WithHttpHealthCheck("/health/ready")
    .WaitFor(catalog)
    .WaitFor(orders)
    .WaitFor(reviews)
    .WaitFor(aggregator)
    .WaitFor(auth)
    .WithExternalHttpEndpoints();

builder.Build().Run();
