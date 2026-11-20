using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Bakery.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/product-ratings")]
public sealed class ProductRatingsController(ISender sender) : ControllerBase
{
    [HttpGet("{productId:int}")]
    [RequirePermission("reviews.read")]
    public async Task<ActionResult<ProductRatingDto>> GetByProduct(int productId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetProductRatingQuery(productId), ct));
    }
}
