using Bakery.Reviews.Domain.Entities;

namespace Bakery.Reviews.Domain.Interfaces;

public interface IProductRatingRepository
{
    Task<ProductRating?> GetByProductIdAsync(int productId, CancellationToken ct = default);

    Task RecalculateAsync(int productId, CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);
}
