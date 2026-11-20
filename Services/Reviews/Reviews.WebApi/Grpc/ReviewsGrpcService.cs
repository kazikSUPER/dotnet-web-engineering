using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Exceptions;
using Grpc.Core;
using MediatR;

namespace Reviews.WebApi.Grpc;

public sealed class ReviewsGrpcService(ISender sender) : ReviewsService.ReviewsServiceBase
{
    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Review id is required."));
        }

        try
        {
            var review = await sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);
            return MapReview(review);
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ProductRatingReply> GetProductRating(GetProductRatingRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be positive."));
        }

        try
        {
            var rating = await sender.Send(new GetProductRatingQuery(request.ProductId), context.CancellationToken);
            return new ProductRatingReply
            {
                ProductId = rating.ProductId,
                TotalReviews = rating.TotalReviews,
                AverageScore = rating.AverageScore,
                LastUpdated = rating.LastUpdated.ToString("O"),
            };
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<ProductReviewsReply> GetProductReviews(GetProductReviewsRequest request, ServerCallContext context)
    {
        if (request.ProductId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Product id must be positive."));
        }

        var page = await sender.Send(new GetReviewsQuery { ProductId = request.ProductId, Limit = 50 }, context.CancellationToken);

        var reply = new ProductReviewsReply();
        foreach (var item in page.Items)
        {
            reply.Items.Add(new ReviewReply
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                CustomerName = item.CustomerName,
                Taste = item.Ratings.Taste,
                Freshness = item.Ratings.Freshness,
                Aroma = item.Ratings.Aroma,
                Average = item.Ratings.Average,
                Status = item.Status,
                CreatedAt = item.CreatedAt.ToString("O"),
            });
        }

        return reply;
    }

    private static ReviewReply MapReview(ReviewDto review) => new()
    {
        Id = review.Id,
        OrderId = review.OrderId,
        CustomerId = review.CustomerId,
        CustomerName = review.CustomerName,
        ProductId = review.ProductId,
        ProductName = review.ProductName,
        Comment = review.Comment,
        Taste = review.Ratings.Taste,
        Freshness = review.Ratings.Freshness,
        Aroma = review.Ratings.Aroma,
        Average = review.Ratings.Average,
        Status = review.Status,
        CreatedAt = review.CreatedAt.ToString("O"),
    };
}
