using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Repositories;

public sealed class ProductRatingRepository(MongoDbContext db) : IProductRatingRepository
{
    public async Task<ProductRating?> GetByProductIdAsync(int productId, CancellationToken ct = default)
    {
        return await db.ProductRatings
            .Find(db.Session, Builders<ProductRating>.Filter.Eq(x => x.ProductId, productId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task RecalculateAsync(int productId, CancellationToken ct = default)
    {
        var stats = await db.Reviews
            .Aggregate(db.Session)
            .Match(x => x.ProductId == productId && x.Status == ReviewStatus.Approved)
            .Group(
                x => x.ProductId,
                group => new
                {
                    TotalReviews = group.Count(),
                    AverageScore = group.Average(review => review.AverageRating),
                })
            .FirstOrDefaultAsync(ct);

        var totalReviews = stats?.TotalReviews ?? 0;
        var averageScore = stats is null ? 0d : Math.Round(stats.AverageScore, 2);
        var now = DateTime.UtcNow;

        var updates = Builders<ProductRating>.Update;

        var update = updates
            .Set(x => x.TotalReviews, totalReviews)
            .Set(x => x.AverageScore, averageScore)
            .Set(x => x.LastUpdated, now)
            .Set(x => x.UpdatedAt, now)
            .Inc(x => x.Version, 1)
            .SetOnInsert(x => x.ProductId, productId)
            .SetOnInsert(x => x.CreatedAt, now);

        await db.ProductRatings.UpdateOneAsync(
            db.Session,
            Builders<ProductRating>.Filter.Eq(x => x.ProductId, productId),
            update,
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default)
    {
        return db.ProductRatings.Find(db.Session, Builders<ProductRating>.Filter.Empty).AnyAsync(ct);
    }
}
