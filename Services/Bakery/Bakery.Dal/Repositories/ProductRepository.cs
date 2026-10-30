using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public class ProductRepository(BakeryDbContext context) : GenericRepository<Product>(context), IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken ct = default) => await Set.AsNoTracking().Where(x => x.ProductCategories.Any(pc => pc.CategoryId == categoryId)).Include(x => x.ProductCategories).ThenInclude(pc => pc.Category).ToListAsync(ct);
}
