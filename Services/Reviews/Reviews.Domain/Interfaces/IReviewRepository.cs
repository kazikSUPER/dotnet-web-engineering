using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.ValueObjects;
using MongoDB.Bson;

namespace Bakery.Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    Task<bool> ExistsForOrderProductAsync(int orderId, int productId, CancellationToken ct = default);

    Task<bool> AnyAsync(CancellationToken ct = default);

    Task AddAsync(Review review, CancellationToken ct = default);

    Task AddManyAsync(IEnumerable<Review> reviews, CancellationToken ct = default);

    Task ReplaceAsync(Review review, int expectedVersion, CancellationToken ct = default);

    Task<bool> DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default);

    Task<long> UpdateProductNameAsync(int productId, string productName, CancellationToken ct = default);

    Task<long> UpdateCustomerNameAsync(int customerId, PersonName name, CancellationToken ct = default);
}
