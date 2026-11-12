using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<IReadOnlyList<ProductInCategoryDto>>> GetByCategory(int categoryId, [FromQuery] DateTime? notEarlierThan, CancellationToken ct) => Ok(await service.GetByCategoryAsync(categoryId, notEarlierThan, ct));
}
