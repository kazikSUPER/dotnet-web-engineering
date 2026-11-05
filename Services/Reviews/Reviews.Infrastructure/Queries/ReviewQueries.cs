using AutoMapper;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Filters;
using Bakery.Reviews.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bakery.Reviews.Infrastructure.Queries;

public sealed class ReviewQueries(MongoDbContext db, IMapper mapper) : IReviewQueries
{
    public async Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        var review = await db.Reviews.Find(db.Session, Builders<Review>.Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync(ct);

        return review is null ? null : mapper.Map<ReviewDto>(review);
    }

    public async Task<ReviewPageDto> GetPageAsync(ReviewFilter filter, string? cursor, int limit, CancellationToken ct = default)
    {
        var builder = Builders<Review>.Filter;
        var conditions = new List<FilterDefinition<Review>>();

        if (filter.ProductId is { } productId)
        {
            conditions.Add(builder.Eq(x => x.ProductId, productId));
        }

        if (filter.CustomerId is { } customerId)
        {
            conditions.Add(builder.Eq(x => x.CustomerId, customerId));
        }

        if (filter.Status is { } status)
        {
            conditions.Add(builder.Eq(x => x.Status, status));
        }

        if (filter.MinAverage is { } minAverage)
        {
            conditions.Add(builder.Gte(x => x.AverageRating, minAverage));
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            conditions.Add(builder.Eq(MongoElements.Tags, filter.Tag));
        }

        if (CursorToken.TryParse(cursor, out var token))
        {
            conditions.Add(BuildCursorCondition(filter, token));
        }

        var documents = await db.Reviews
            .Find(db.Session, Combine(builder, conditions))
            .Sort(BuildSort(filter))
            .Limit(limit + 1)
            .ToListAsync(ct);

        var hasMore = documents.Count > limit;
        var page = hasMore ? documents.Take(limit).ToArray() : documents.ToArray();
        var items = page.Select(mapper.Map<ReviewListItemDto>).ToArray();

        var nextCursor = hasMore && page.Length > 0
            ? BuildCursor(page[^1], filter).ToString()
            : null;

        return new ReviewPageDto
        {
            Items = items,
            NextCursor = nextCursor,
            HasMore = hasMore,
        };
    }

    public async Task<IReadOnlyList<ReviewListItemDto>> SearchAsync(string term, int limit, CancellationToken ct = default)
    {
        var documents = await db.Reviews
            .Find(db.Session, Builders<Review>.Filter.Text(term))
            .Sort(Builders<Review>.Sort.Descending(x => x.CreatedAt))
            .Limit(limit)
            .ToListAsync(ct);

        return documents.Select(mapper.Map<ReviewListItemDto>).ToArray();
    }

    public async Task<ObjectId?> FindIdByOrderProductAsync(int orderId, int productId, CancellationToken ct = default)
    {
        var builder = Builders<Review>.Filter;

        return await db.Reviews
            .Find(db.Session, builder.Eq(x => x.OrderId, orderId) & builder.Eq(x => x.ProductId, productId))
            .Project(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static FilterDefinition<Review> Combine(
        FilterDefinitionBuilder<Review> builder,
        IReadOnlyList<FilterDefinition<Review>> conditions)
    {
        return conditions.Count switch
        {
            0 => builder.Empty,
            1 => conditions[0],
            _ => builder.And(conditions),
        };
    }

    private static FilterDefinition<Review> BuildCursorCondition(ReviewFilter filter, CursorToken token)
    {
        if (filter.Sort == ReviewSortField.AverageRating)
        {
            return BuildCursorForRating(filter, token);
        }

        return BuildCursorForDate(filter, token);
    }

    private static FilterDefinition<Review> BuildCursorForDate(ReviewFilter filter, CursorToken token)
    {
        var builder = Builders<Review>.Filter;
        var createdAt = CursorToken.ToCreatedAt(token.SortValue);

        var beyond = filter.Descending
            ? builder.Lt(x => x.CreatedAt, createdAt)
            : builder.Gt(x => x.CreatedAt, createdAt);

        var sameTime = builder.Eq(x => x.CreatedAt, createdAt);
        var tieBreaker = filter.Descending
            ? builder.Lt(x => x.Id, token.Id)
            : builder.Gt(x => x.Id, token.Id);

        return builder.Or(beyond, builder.And(sameTime, tieBreaker));
    }

    private static FilterDefinition<Review> BuildCursorForRating(ReviewFilter filter, CursorToken token)
    {
        var builder = Builders<Review>.Filter;
        var averageRating = CursorToken.ToAverageRating(token.SortValue);

        var beyond = filter.Descending
            ? builder.Lt(x => x.AverageRating, averageRating)
            : builder.Gt(x => x.AverageRating, averageRating);

        var sameScore = builder.Eq(x => x.AverageRating, averageRating);
        var tieBreaker = filter.Descending
            ? builder.Lt(x => x.Id, token.Id)
            : builder.Gt(x => x.Id, token.Id);

        return builder.Or(beyond, builder.And(sameScore, tieBreaker));
    }

    private static SortDefinition<Review> BuildSort(ReviewFilter filter)
    {
        var builder = Builders<Review>.Sort;

        if (filter.Sort == ReviewSortField.AverageRating)
        {
            return filter.Descending
                ? builder.Descending(x => x.AverageRating).Descending(x => x.Id)
                : builder.Ascending(x => x.AverageRating).Ascending(x => x.Id);
        }

        return filter.Descending
            ? builder.Descending(x => x.CreatedAt).Descending(x => x.Id)
            : builder.Ascending(x => x.CreatedAt).Ascending(x => x.Id);
    }

    private static CursorToken BuildCursor(Review last, ReviewFilter filter)
    {
        return filter.Sort == ReviewSortField.AverageRating
            ? CursorToken.Create(last.AverageRating, last.Id)
            : CursorToken.Create(last.CreatedAt, last.Id);
    }
}
