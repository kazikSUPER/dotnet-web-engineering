using Catalog.DAL.Data;
using Catalog.DAL.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.DAL.Repositories;

public class ProductRepository(CatalogDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default) => await Set.AsNoTracking().Where(x => x.ProductCategories.Any(pc => pc.CategoryId == categoryId)).Include(x => x.ProductCategories).ThenInclude(pc => pc.Category).ToListAsync(ct);

    public async Task<IReadOnlyList<ProductCategory>> GetCategoryLinksAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default) => await Context.Set<ProductCategory>()
        .AsNoTracking()
        .Where(pc => pc.CategoryId == categoryId && (!notEarlierThan.HasValue || pc.AddedAt >= notEarlierThan))
        .Include(pc => pc.Product)
        .Include(pc => pc.Category)
        .OrderBy(pc => pc.AddedAt)
        .ToListAsync(ct);
}
