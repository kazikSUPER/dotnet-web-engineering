using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.Diagnostics;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Publishing;
using System.Diagnostics;

namespace Bakery.Reviews.Application.Commands;

public sealed record CreateReviewCommand(
    int OrderId,
    int CustomerId,
    string CustomerName,
    int ProductId,
    string ProductName,
    string Comment,
    int Taste,
    int Freshness,
    int Aroma,
    IReadOnlyList<string>? Tags = null) : ICommand<ReviewDto>;

public sealed class CreateReviewCommandHandler(
    IReviewRepository reviews,
    IProductRatingRepository ratings,
    IReviewQueries queries,
    ICacheService cache,
    IEventPublisher eventPublisher,
    ILogger<CreateReviewCommandHandler> logger) : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        using var activity = ReviewsActivitySources.Commands.StartActivity("review.create", ActivityKind.Internal);

        var review = Review.Create(
            request.OrderId,
            request.CustomerId,
            request.CustomerName,
            request.ProductId,
            request.ProductName,
            request.Comment,
            request.Taste,
            request.Freshness,
            request.Aroma,
            request.Tags);

        await reviews.AddAsync(review, ct);

        // 6. Publish event ONLY after successful commit
        try
        {
            var rating = await ratings.GetByProductIdAsync(review.ProductId, ct);
            await eventPublisher.PublishAsync(new ReviewCreatedEvent(
                review.Id.ToString(),
                review.ProductId,
                review.CustomerId,
                (int)Math.Round(review.Ratings.Average),
                rating?.AverageScore ?? review.Ratings.Average,
                rating?.TotalReviews ?? 1), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish ReviewCreatedEvent for review {ReviewId}", review.Id);
        }

        activity?.SetTag("review.id", review.Id.ToString());
        activity?.SetTag("review.product_id", review.ProductId);
        activity?.SetTag("review.order_id", review.OrderId);
        activity?.SetTag("review.status", review.Status.ToString());
        activity?.SetTag("review.average", review.Ratings.Average);

        logger.LogInformation(
            "Відгук {ReviewId} створено для товару {ProductId} із замовлення {OrderId}: статус {Status}, оцінка {Average}",
            review.Id, review.ProductId, review.OrderId, review.Status, review.Ratings.Average);

        const string reason = "review-created";
        await cache.RemoveAsync(CacheKeys.ProductRating(request.ProductId), reason, ct);
        await cache.RemoveAsync(CacheKeys.ProductReviews(request.ProductId), reason, ct);
        await cache.RemoveAsync(CacheKeys.AggregateOrderFull(request.OrderId), reason, ct);

        return await queries.GetByIdAsync(review.Id, ct)
            ?? throw new ConflictException($"Відгук {review.Id} створено, але читальна модель його не повернула.");
    }
}
