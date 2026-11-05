using Bakery.Reviews.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Persistence;

public sealed class MongoDbContext(
    IMongoClient client,
    IOptions<MongoDbOptions> options,
    IMongoSessionAccessor sessions) : IDisposable
{
    private readonly MongoDbOptions settings = options.Value;

    private IClientSessionHandle? scopeSession;

    public IMongoDatabase Database { get; } = client.GetDatabase(options.Value.DatabaseName);

    public IMongoSessionAccessor Sessions => sessions;

    public IClientSessionHandle Session
    {
        get
        {
            var ambient = sessions.Current;

            if (ambient is not null)
            {
                return ambient;
            }

            return scopeSession ??= client.StartSession();
        }
    }

    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>(settings.ReviewsCollectionName);

    public IMongoCollection<ProductRating> ProductRatings =>
        Database.GetCollection<ProductRating>(settings.ProductRatingsCollectionName);

    public IMongoCollection<ModerationLog> ModerationLogs =>
        Database.GetCollection<ModerationLog>(settings.ModerationLogsCollectionName);

    public void Dispose()
    {
        scopeSession?.Dispose();
        scopeSession = null;
    }
}
