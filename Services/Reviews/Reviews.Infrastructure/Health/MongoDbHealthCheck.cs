using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Health;

public sealed class MongoDbHealthCheck(MongoDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var count = await db.Reviews.CountDocumentsAsync(db.Session, Builders<Review>.Filter.Empty, null, ct);

            return HealthCheckResult.Healthy($"MongoDB {db.Database.DatabaseNamespace.DatabaseName} доступний, відгуків: {count}");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("MongoDB не відповідає", exception);
        }
    }
}
