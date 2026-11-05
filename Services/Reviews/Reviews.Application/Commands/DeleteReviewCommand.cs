using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Domain.Exceptions;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace Bakery.Reviews.Application.Commands;

public sealed record DeleteReviewCommand(string Id, int ExpectedVersion = 1) : ICommand;

public sealed class DeleteReviewCommandHandler(
    IReviewRepository reviews,
    IProductRatingRepository ratings,
    IMongoUnitOfWork unitOfWork) : IRequestHandler<DeleteReviewCommand, Unit>
{
    public async Task<Unit> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var id = ObjectId.Parse(request.Id);
        var review = await reviews.GetByIdAsync(id, ct) ?? throw new NotFoundException("Відгук", request.Id);
        var productId = review.ProductId;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (!await reviews.DeleteAsync(id, request.ExpectedVersion, token))
            {
                throw ConflictException.Concurrency(review, request.ExpectedVersion);
            }

            await ratings.RecalculateAsync(productId, token);
        }, ct);

        return Unit.Value;
    }
}
