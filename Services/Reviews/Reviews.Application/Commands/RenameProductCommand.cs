using Bakery.Reviews.Application.Caching;
using Bakery.Reviews.Application.Common;
using Bakery.Reviews.Domain.Interfaces;
using MediatR;

namespace Bakery.Reviews.Application.Commands;

public sealed record RenameProductCommand(int ProductId, string NewProductName) : ICommand<int>;

public sealed class RenameProductCommandHandler(IReviewRepository reviews, ICacheService cache)
    : IRequestHandler<RenameProductCommand, int>
{
    public async Task<int> Handle(RenameProductCommand request, CancellationToken ct)
    {
        var updated = await reviews.UpdateProductNameAsync(request.ProductId, request.NewProductName, ct);
        var ids = await reviews.GetIdsByProductAsync(request.ProductId, ct);

        const string reason = "product-renamed";
        foreach (var id in ids)
        {
            await cache.RemoveAsync(CacheKeys.Review(id), reason, ct);
        }

        await cache.RemoveAsync(CacheKeys.ProductReviews(request.ProductId), reason, ct);

        return (int)updated;
    }
}
