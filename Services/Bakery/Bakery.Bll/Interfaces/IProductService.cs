using Bakery.BLL.DTOs.Products;

namespace Bakery.BLL.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default);
}
