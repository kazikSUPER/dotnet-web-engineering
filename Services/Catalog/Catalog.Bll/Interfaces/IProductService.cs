using Catalog.BLL.DTOs.Products;

namespace Catalog.BLL.Interfaces;

public interface IProductService
{
    Task<ProductCardDto?> GetCardAsync(int productId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(string name, decimal price, int stock, int categoryId, string categoryName, CancellationToken ct = default);
    Task<ProductDto?> RenameProductAsync(int productId, string newName, CancellationToken ct = default);
    Task<ProductDto?> UpdatePriceAsync(int productId, decimal newPrice, CancellationToken ct = default);
}
