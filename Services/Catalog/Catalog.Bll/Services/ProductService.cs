using AutoMapper;
using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Catalog.DAL.Interfaces;

namespace Catalog.BLL.Services;

public class ProductService(IUnitOfWork uow, IMapper mapper) : IProductService
{
    public async Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default)
    {
        var links = await uow.Products.GetCategoryLinksAsync(categoryId, notEarlierThan, ct);
        return mapper.Map<IReadOnlyList<ProductInCategoryDto>>(links);
    }
}
