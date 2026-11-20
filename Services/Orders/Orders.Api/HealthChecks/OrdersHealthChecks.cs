using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.Api.HealthChecks;

public static class OrdersHealthChecks
{
    public static IServiceCollection AddOrdersHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("OrdersDb") ?? string.Empty,
                healthQuery: "SELECT 1;",
                name: "orders-sql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["database", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString("redis") ?? "localhost:6379",
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "ready"],
                timeout: TimeSpan.FromSeconds(3));

        services.AddGrpcHealthChecks();

        return services;
    }
}
