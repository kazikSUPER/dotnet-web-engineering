using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;

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
    IReviewQueries queries) : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
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

        return await queries.GetByIdAsync(review.Id, ct)
            ?? throw new ConflictException($"Відгук {review.Id} створено, але читальна модель його не повернула.");
    }
}
