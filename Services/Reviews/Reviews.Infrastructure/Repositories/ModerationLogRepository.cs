using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Filters;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Repositories;

public sealed class ModerationLogRepository(MongoDbContext db) : IModerationLogRepository
{
    public Task AddAsync(ModerationLog log, CancellationToken ct = default)
    {
        return db.ModerationLogs.InsertOneAsync(db.Session, log, ct);
    }

    public Task<bool> AnyAsync(CancellationToken ct = default)
    {
        return db.ModerationLogs.Find(db.Session, Builders<ModerationLog>.Filter.Empty).AnyAsync(ct);
    }

    public async Task<CursorPage<ModerationLog>> GetPageAsync(ObjectId? reviewId, string? cursor, int limit, CancellationToken ct = default)
    {
        var filters = Builders<ModerationLog>.Filter;
        var conditions = new List<FilterDefinition<ModerationLog>>();

        if (reviewId is not null)
        {
            conditions.Add(filters.Eq(x => x.ReviewId, reviewId));
        }

        if (CursorToken.TryParse(cursor, out var token))
        {
            var createdAt = CursorToken.ToCreatedAt(token.SortValue);

            var older = filters.Lt(x => x.Timestamp, createdAt);
            var sameMoment = filters.And(filters.Eq(x => x.Timestamp, createdAt), filters.Lt(x => x.Id, token.Id));

            conditions.Add(filters.Or(older, sameMoment));
        }

        var filter = conditions.Count == 0 ? filters.Empty : filters.And(conditions);
        var sort = Builders<ModerationLog>.Sort.Descending(x => x.Timestamp).Descending(x => x.Id);

        var documents = await db.ModerationLogs
            .Find(db.Session, filter)
            .Sort(sort)
            .Limit(limit + 1)
            .ToListAsync(ct);

        var hasMore = documents.Count > limit;
        var items = (hasMore ? documents.Take(limit) : documents).ToArray();

        var nextCursor = hasMore && items.Length > 0
            ? CursorToken.Create(items[^1].Timestamp, items[^1].Id).ToString()
            : null;

        return new CursorPage<ModerationLog>(items, nextCursor, hasMore);
    }
}
