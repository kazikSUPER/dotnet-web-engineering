using Catalog.Domain.Entities;

namespace Catalog.DAL.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetCardAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductCategory>> GetCategoryLinksAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default);
}
