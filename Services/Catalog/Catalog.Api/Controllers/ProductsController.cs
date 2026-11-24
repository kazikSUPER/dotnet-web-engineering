using System.Diagnostics;
using Catalog.Api.Metrics;
using Catalog.BLL.DTOs.Products;
using Catalog.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "catalog.read")]
public class ProductsController(IProductService service, CatalogMetrics metrics) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission("products.read")]
    public async Task<ActionResult<ProductCardDto>> GetCard(int id, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await service.GetCardAsync(id, ct);
            metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "get_card", "success");
            return result is null ? NotFound() : Ok(result);
        }
        catch (Exception)
        {
            metrics.RecordDuration(sw.Elapsed.TotalMilliseconds, "get_card", "failure");
            throw;
        }
    }

    [HttpGet("category/{categoryId:int}")]
    [RequirePermission("categories.read")]
    public async Task<ActionResult<IReadOnlyList<ProductInCategoryDto>>> GetByCategory(int categoryId, [FromQuery] DateTime? notEarlierThan, CancellationToken ct) => Ok(await service.GetByCategoryAsync(categoryId, notEarlierThan, ct));

    [HttpPost]
    [RequirePermission("products.write")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "Назва товару є обов'язковою.",
                Instance = HttpContext.Request.Path
            });
        }
        if (request.Price <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "Ціна товару має бути більшою за нуль.",
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            var product = await service.CreateProductAsync(request.Name, request.Price, request.Stock, request.CategoryId, request.CategoryName, ct);
            return CreatedAtAction(nameof(GetCard), new { id = product.Id }, product);
        }
        catch (Catalog.Domain.Exceptions.DomainValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
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
