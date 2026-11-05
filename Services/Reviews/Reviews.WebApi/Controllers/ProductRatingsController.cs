using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/v1/product-ratings")]
public sealed class ProductRatingsController(ISender sender) : ControllerBase
{
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductRatingDto>> GetByProduct(int productId, CancellationToken ct)
    {
        return Ok(await sender.Send(new GetProductRatingQuery(productId), ct));
    }
}
