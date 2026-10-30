using Bakery.Domain.Entities;

namespace Bakery.DAL.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);
}
