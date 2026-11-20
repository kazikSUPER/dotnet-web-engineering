using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.Diagnostics;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Enums;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Publishing;
using System.Diagnostics;

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
    IReviewQueries queries,
    ICacheService cache,
    IEventPublisher eventPublisher,
    ILogger<ModerateReviewCommandHandler> logger) : IRequestHandler<ModerateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(ModerateReviewCommand request, CancellationToken ct)
    {
        using var activity = ReviewsActivitySources.Commands.StartActivity("review.moderate", ActivityKind.Internal);
        activity?.SetTag("review.id", request.Id);
        activity?.SetTag("review.moderation_action", request.Action.ToString());
        activity?.SetTag("review.moderator", request.Moderator);

        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);

        var expectedVersion = review.Version;
        var previousStatus = review.Status;

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

        // 6. Publish event ONLY after successful MongoDB transaction commit
        try
        {
            var updatedRating = await ratings.GetByProductIdAsync(review.ProductId, ct);
            await eventPublisher.PublishAsync(new ReviewModeratedEvent(
                review.Id.ToString(),
                review.ProductId,
                request.Action.ToString(),
                updatedRating?.AverageScore ?? 0,
                updatedRating?.TotalReviews ?? 0), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish ReviewModeratedEvent for review {ReviewId}", review.Id);
        }

        activity?.SetTag("review.product_id", review.ProductId);
        activity?.SetTag("review.status", review.Status.ToString());
        activity?.SetTag("review.status_previous", previousStatus.ToString());

        logger.LogInformation(
            "Відгук {ReviewId} товару {ProductId} модератор {Moderator} перевів зі статусу {PreviousStatus} у {Status}",
            review.Id, review.ProductId, request.Moderator, previousStatus, review.Status);

        var reason = $"review-moderated:{request.Action}";
        await cache.RemoveAsync(CacheKeys.Review(request.Id), reason, ct);
        await cache.RemoveAsync(CacheKeys.ProductRating(review.ProductId), reason, ct);
        await cache.RemoveAsync(CacheKeys.ProductReviews(review.ProductId), reason, ct);
        await cache.RemoveAsync(CacheKeys.AggregateOrderFull(review.OrderId), reason, ct);

        return await queries.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);
    }
}
