using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using MediatR;

namespace Bakery.Reviews.Application.Queries;

public sealed record SearchReviewsQuery(string Text, int Limit = 20) : IQuery<IReadOnlyList<ReviewListItemDto>>;

public sealed class SearchReviewsQueryHandler(IReviewQueries queries) : IRequestHandler<SearchReviewsQuery, IReadOnlyList<ReviewListItemDto>>
{
    public Task<IReadOnlyList<ReviewListItemDto>> Handle(SearchReviewsQuery request, CancellationToken ct)
    {
        return queries.SearchAsync(request.Text.Trim(), request.Limit, ct);
    }
}
