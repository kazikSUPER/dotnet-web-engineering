using Catalog.BLL.DTOs.Products;

namespace Catalog.BLL.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default);
}
