namespace Bakery.Reviews.Infrastructure.Persistence;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = "BakeryReviewsDB";

    public string ReviewsCollectionName { get; init; } = "reviews";

    public string ProductRatingsCollectionName { get; init; } = "product_ratings";

    public string ModerationLogsCollectionName { get; init; } = "moderation_logs";

    public bool UseTransactions { get; init; } = true;

    public bool CreateIndexesOnStartup { get; init; } = true;

    public bool SeedOnStartup { get; init; } = true;

    public int ServerSelectionTimeoutMilliseconds { get; init; } = 5000;
}
