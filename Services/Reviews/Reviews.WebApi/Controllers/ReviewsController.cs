using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public class ReviewsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReviewPageDto>> GetList([FromQuery] GetReviewsQuery query, CancellationToken ct)
    {
        return Ok(await sender.Send(query, ct));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ReviewListItemDto>>> Search(
        [FromQuery] string text,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        return Ok(await sender.Send(new SearchReviewsQuery(text, limit), ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await sender.Send(new GetReviewByIdQuery(id), ct);

        Response.Headers.ETag = ETagHelper.Format(review.Version);

        return Ok(review);
    }

    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewCommand command, CancellationToken ct)
    {
        var review = await sender.Send(command, ct);

        Response.Headers.ETag = ETagHelper.Format(review.Version);

        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ReviewDto>> Update(string id, [FromBody] UpdateReviewCommand command, CancellationToken ct)
    {
        var expectedVersion = ETagHelper.Parse(Request.Headers.IfMatch.FirstOrDefault()) ?? command.ExpectedVersion;

        var review = await sender.Send(command with { Id = id, ExpectedVersion = expectedVersion }, ct);

        Response.Headers.ETag = ETagHelper.Format(review.Version);

        return Ok(review);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] int expectedVersion = 1, CancellationToken ct = default)
    {
        var version = ETagHelper.Parse(Request.Headers.IfMatch.FirstOrDefault()) ?? expectedVersion;

        await sender.Send(new DeleteReviewCommand(id, version), ct);

        return NoContent();
    }

    [HttpPut("by-product/{productId:int}")]
    public async Task<ActionResult<int>> RenameProduct(int productId, [FromBody] RenameProductCommand command, CancellationToken ct)
    {
        return Ok(await sender.Send(command with { ProductId = productId }, ct));
    }

    [HttpPut("by-customer/{customerId:int}")]
    public async Task<ActionResult<int>> ChangeCustomerName(int customerId, [FromBody] ChangeCustomerNameCommand command, CancellationToken ct)
    {
        return Ok(await sender.Send(command with { CustomerId = customerId }, ct));
    }
}
