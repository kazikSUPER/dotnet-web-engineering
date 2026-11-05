using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Commands;

public sealed record UpdateReviewCommand(
    string Id,
    string Comment,
    int Taste,
    int Freshness,
    int Aroma,
    IReadOnlyList<string>? Tags = null,
    int ExpectedVersion = 1) : ICommand<ReviewDto>;

public sealed class UpdateReviewCommandHandler(
    IReviewRepository reviews,
    IReviewQueries queries) : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken ct)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);

        review.UpdateContent(new ReviewComment(request.Comment), new Rating(request.Taste, request.Freshness, request.Aroma), request.Tags);
        await reviews.ReplaceAsync(review, request.ExpectedVersion, ct);

        return await queries.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);
    }
}
