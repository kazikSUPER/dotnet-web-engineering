using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using Bakery.Reviews.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Commands;

public sealed record AddAdminResponseCommand(
    string Id,
    string ModeratorId,
    string Message,
    int ExpectedVersion = 1) : ICommand<ReviewDto>;

public sealed class AddAdminResponseCommandHandler(
    IReviewRepository reviews,
    IReviewQueries queries) : IRequestHandler<AddAdminResponseCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(AddAdminResponseCommand request, CancellationToken ct)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);

        review.AddAdminResponse(new AdminResponse(request.ModeratorId, request.Message, DateTime.UtcNow));
        await reviews.ReplaceAsync(review, request.ExpectedVersion, ct);

        return await queries.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);
    }
}
