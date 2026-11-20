using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission("products.read")]
    public async Task<ActionResult<ProductCardDto>> GetCard(int id, CancellationToken ct)
    {
        var result = await service.GetCardAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("category/{categoryId:int}")]
    [RequirePermission("categories.read")]
    public async Task<ActionResult<IReadOnlyList<ProductInCategoryDto>>> GetByCategory(int categoryId, [FromQuery] DateTime? notEarlierThan, CancellationToken ct) => Ok(await service.GetByCategoryAsync(categoryId, notEarlierThan, ct));

    [HttpPost]
    [RequirePermission("products.write")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = await service.CreateProductAsync(request.Name, request.Price, request.Stock, request.CategoryId, request.CategoryName, ct);
        return CreatedAtAction(nameof(GetCard), new { id = product.Id }, product);
    }

    [HttpPatch("{id:int}/name")]
    [RequirePermission("products.write")]
    public async Task<IActionResult> Rename(int id, [FromBody] RenameProductRequest request, CancellationToken ct)
    {
        var product = await service.RenameProductAsync(id, request.NewName, ct);
        return product is not null ? Ok(product) : NotFound();
    }

    [HttpPatch("{id:int}/price")]
    [RequirePermission("products.write")]
    public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdatePriceRequest request, CancellationToken ct)
    {
        var product = await service.UpdatePriceAsync(id, request.NewPrice, ct);
        return product is not null ? Ok(product) : NotFound();
    }
}

public record CreateProductRequest(string Name, decimal Price, int Stock, int CategoryId, string CategoryName);
public record RenameProductRequest(string NewName);
public record UpdatePriceRequest(decimal NewPrice);
