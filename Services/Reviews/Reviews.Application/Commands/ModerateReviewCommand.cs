using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Commands;

public sealed record ModerateReviewCommand(
    string Id,
    ModerationAction Action,
    string Moderator,
    string? Notes = null) : ICommand<ReviewDto>;

public sealed class ModerateReviewCommandHandler(
    IReviewRepository reviews,
    IProductRatingRepository ratings,
    IModerationLogRepository logs,
    IMongoUnitOfWork unitOfWork,
    IReviewQueries queries) : IRequestHandler<ModerateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(ModerateReviewCommand request, CancellationToken ct)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);

        var expectedVersion = review.Version;

        switch (request.Action)
        {
            case ModerationAction.Approved:
                review.Approve();
                break;
            case ModerationAction.Rejected:
                review.Reject();
                break;
            case ModerationAction.Flagged:
                review.Flag();
                break;
            case ModerationAction.Replied:
                throw new ConflictException("Відповідь адміністратора додається окремою командою.");
            default:
                throw new DomainException($"Невідома дія модератора {request.Action}.");
        }

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await reviews.ReplaceAsync(review, expectedVersion, token);
            await logs.AddAsync(new ModerationLog(review.Id, request.Action, request.Moderator, request.Notes), token);
            await ratings.RecalculateAsync(review.ProductId, token);
        }, ct);

        return await queries.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);
    }
}
