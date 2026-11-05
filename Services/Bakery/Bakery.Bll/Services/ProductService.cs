using AutoMapper;
using Bakery.BLL.DTOs.Products;
using Bakery.BLL.Interfaces;
using Bakery.DAL.Interfaces;

namespace Bakery.BLL.Services;

public class ProductService(IUnitOfWork uow, IMapper mapper) : IProductService
{
    public async Task<IReadOnlyList<ProductInCategoryDto>> GetByCategoryAsync(int categoryId, DateTime? notEarlierThan = null, CancellationToken ct = default)
    {
        var links = await uow.Products.GetCategoryLinksAsync(categoryId, notEarlierThan, ct);
        return mapper.Map<IReadOnlyList<ProductInCategoryDto>>(links);
    }
}
