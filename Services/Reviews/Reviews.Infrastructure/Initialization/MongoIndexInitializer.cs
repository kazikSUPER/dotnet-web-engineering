using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Initialization;

public sealed class MongoIndexInitializer(MongoDbContext db, ILogger<MongoIndexInitializer> logger)
{
    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await EnsureReviewsIndexesAsync(ct);
        await EnsureProductRatingIndexesAsync(ct);
        await EnsureModerationLogIndexesAsync(ct);

        logger.LogInformation("Індекси MongoDB перевірено та створено за потреби");
    }

    private async Task EnsureReviewsIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<Review>.IndexKeys;

        var models = new List<CreateIndexModel<Review>>
        {
            new(
                keys.Ascending(x => x.ProductId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_reviews_product_created" }),

            new(
                keys.Ascending(x => x.CustomerId),
                new CreateIndexOptions { Name = "ix_reviews_customer" }),

            new(
                keys.Ascending(x => x.Status).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_reviews_status_created" }),

            new(
                keys.Descending(x => x.AverageRating).Descending(x => x.Id),
                new CreateIndexOptions { Name = "ix_reviews_rating" }),

            new(
                keys.Ascending(x => x.OrderId).Ascending(x => x.ProductId),
                new CreateIndexOptions { Name = "ux_reviews_order_product", Unique = true }),

            new(
                keys.Text(x => x.Comment),
                new CreateIndexOptions { Name = "tx_reviews_comment", DefaultLanguage = "none" }),
        };

        await db.Reviews.Indexes.CreateManyAsync(models, ct);
    }

    private async Task EnsureProductRatingIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<ProductRating>.IndexKeys;

        await db.ProductRatings.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<ProductRating>(
                    keys.Ascending(x => x.ProductId),
                    new CreateIndexOptions { Name = "ux_product_ratings_product", Unique = true }),
            ],
            ct);
    }

    private async Task EnsureModerationLogIndexesAsync(CancellationToken ct)
    {
        var keys = Builders<ModerationLog>.IndexKeys;

        var models = new List<CreateIndexModel<ModerationLog>>
        {
            new(
                keys.Ascending(x => x.ReviewId).Descending(x => x.Timestamp),
                new CreateIndexOptions { Name = "ix_logs_review_timestamp" }),

            new(
                keys.Descending(x => x.Timestamp),
                new CreateIndexOptions { Name = "ix_logs_timestamp" }),
        };

        await db.ModerationLogs.Indexes.CreateManyAsync(models, ct);
    }
}
