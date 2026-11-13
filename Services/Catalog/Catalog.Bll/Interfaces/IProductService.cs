using Catalog.BLL.DTOs.Products;

namespace Catalog.BLL.Interfaces;

public interface IProductService
{
    Task<ProductCardDto?> GetCardAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default);
}
