using Bakery.BLL.DTOs.Products;
using Bakery.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet("category/{categoryId:int}")]
    public async Task<ActionResult<IReadOnlyList<ProductInCategoryDto>>> GetByCategory(int categoryId, [FromQuery] DateTime? notEarlierThan, CancellationToken ct) => Ok(await service.GetByCategoryAsync(categoryId, notEarlierThan, ct));
}
