using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Filters;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Queries;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    Task<ReviewPageDto> GetPageAsync(ReviewFilter filter, string? cursor, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<ReviewListItemDto>> SearchAsync(string term, int limit, CancellationToken ct = default);

    Task<ObjectId?> FindIdByOrderProductAsync(int orderId, int productId, CancellationToken ct = default);
}
