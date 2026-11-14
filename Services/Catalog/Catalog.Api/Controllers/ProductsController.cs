using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductCardDto>> GetCard(int id, CancellationToken ct)
    {
        var result = await service.GetCardAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<IReadOnlyList<ProductInCategoryDto>>> GetByCategory(int categoryId, [FromQuery] DateTime? notEarlierThan, CancellationToken ct) => Ok(await service.GetByCategoryAsync(categoryId, notEarlierThan, ct));
}
