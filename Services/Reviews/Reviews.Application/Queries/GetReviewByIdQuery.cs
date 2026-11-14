using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Exceptions;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Queries;

public sealed record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Review(Id);

    public TimeSpan Ttl => TimeSpan.FromMinutes(5);
}

public sealed class GetReviewByIdQueryHandler(IReviewQueries queries) : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken ct)
    {
        var review = await queries.GetByIdAsync(ObjectId.Parse(request.Id), ct);

        return review ?? throw new NotFoundException("Відгук", request.Id);
    }
}
