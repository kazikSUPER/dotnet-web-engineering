using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Filters;
using MediatR;

namespace Bakery.Reviews.Application.Queries;

public sealed record GetReviewsQuery : IQuery<ReviewPageDto>
{
    public int? ProductId { get; init; }

    public int? CustomerId { get; init; }

    public ReviewStatus? Status { get; init; }

    public double? MinAverage { get; init; }

    public string? Tag { get; init; }

    public ReviewSortField Sort { get; init; } = ReviewSortField.CreatedAt;

    public bool Descending { get; init; } = true;

    public string? Cursor { get; init; }

    public int Limit { get; init; } = 20;
}

public sealed class GetReviewsQueryHandler(IReviewQueries queries) : IRequestHandler<GetReviewsQuery, ReviewPageDto>
{
    public Task<ReviewPageDto> Handle(GetReviewsQuery request, CancellationToken ct)
    {
        var filter = new ReviewFilter
        {
            ProductId = request.ProductId,
            CustomerId = request.CustomerId,
            Status = request.Status,
            MinAverage = request.MinAverage,
            Tag = request.Tag,
            Sort = request.Sort,
            Descending = request.Descending,
        };

        return queries.GetPageAsync(filter, request.Cursor, request.Limit, ct);
    }
}
