using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using Bakery.Reviews.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Repositories;

public sealed class ReviewRepository(MongoDbContext db) : IReviewRepository
{
    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        return await db.Reviews.Find(db.Session, Builders<Review>.Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync(ct);
    }

    public Task<bool> ExistsForOrderProductAsync(int orderId, int productId, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter;

        return db.Reviews
            .Find(db.Session, filter.Eq(x => x.OrderId, orderId) & filter.Eq(x => x.ProductId, productId))
            .AnyAsync(ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default)
    {
        return db.Reviews.Find(db.Session, Builders<Review>.Filter.Empty).AnyAsync(ct);
    }

    public Task AddAsync(Review review, CancellationToken ct = default)
    {
        return db.Reviews.InsertOneAsync(db.Session, review, ct);
    }

    public Task AddManyAsync(IEnumerable<Review> reviews, CancellationToken ct = default)
    {
        return db.Reviews.InsertManyAsync(db.Session, reviews, ct);
    }

    public async Task ReplaceAsync(Review review, int expectedVersion, CancellationToken ct = default)
    {
        var filters = Builders<Review>.Filter;
        var updates = Builders<Review>.Update;

        var filter = filters.Eq(x => x.Id, review.Id) & filters.Eq(x => x.Version, expectedVersion);

        var update = updates
            .Set(x => x.Comment, review.Comment)
            .Set(x => x.Ratings, review.Ratings)
            .Set(x => x.AverageRating, review.AverageRating)
            .Set(MongoElements.Tags, review.Tags.ToList())
            .Set(x => x.Status, review.Status)
            .Set(x => x.AdminResponse, review.AdminResponse)
            .Set(x => x.ProductName, review.ProductName)
            .Set(x => x.CustomerName, review.CustomerName)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Inc(x => x.Version, 1);

        var result = await db.Reviews.UpdateOneAsync(db.Session, filter, update, ct);

        if (result.MatchedCount == 0)
        {
            throw ConflictException.Concurrency(review, expectedVersion);
        }
    }

    public async Task<bool> DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default)
    {
        var filters = Builders<Review>.Filter;
        var result = await db.Reviews.DeleteOneAsync(db.Session, filters.Eq(x => x.Id, id) & filters.Eq(x => x.Version, expectedVersion), ct);

        return result.DeletedCount > 0;
    }

    public async Task<long> UpdateProductNameAsync(int productId, string productName, CancellationToken ct = default)
    {
        var updates = Builders<Review>.Update;
        var update = updates.Set(x => x.ProductName, productName.Trim()).Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await db.Reviews.UpdateManyAsync(
            db.Session,
            Builders<Review>.Filter.Eq(x => x.ProductId, productId),
            update,
            ct);

        return result.ModifiedCount;
    }

    public async Task<long> UpdateCustomerNameAsync(int customerId, PersonName name, CancellationToken ct = default)
    {
        var updates = Builders<Review>.Update;
        var update = updates.Set(x => x.CustomerName, name).Set(x => x.UpdatedAt, DateTime.UtcNow);

        var result = await db.Reviews.UpdateManyAsync(
            db.Session,
            Builders<Review>.Filter.Eq(x => x.CustomerId, customerId),
            update,
            ct);

        return result.ModifiedCount;
    }
}
