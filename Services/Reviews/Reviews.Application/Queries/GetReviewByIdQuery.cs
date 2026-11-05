using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Exceptions;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Queries;

public sealed record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>;

public sealed class GetReviewByIdQueryHandler(IReviewQueries queries) : IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken ct)
    {
        var review = await queries.GetByIdAsync(ObjectId.Parse(request.Id), ct);

        return review ?? throw new NotFoundException("Відгук", request.Id);
    }
}
