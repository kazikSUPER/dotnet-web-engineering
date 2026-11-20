using Bakery.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Reviews.WebApi.HealthChecks;

public static class ReviewsHealthChecks
{
    public static IServiceCollection AddReviewsHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddMongoDb(
                sp => sp.GetRequiredService<MongoDbContext>().Database,
                name: "reviews-mongo",
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
