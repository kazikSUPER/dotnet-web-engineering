using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.Reviews.WebApi.Controllers;

[ApiController]
[Route("api/v1/moderation")]
public sealed class ModerationController(ISender sender) : ControllerBase
{
    [HttpPost("reviews/{id}")]
    public async Task<ActionResult<ReviewDto>> Moderate(
        string id,
        [FromBody] ModerateReviewCommand command,
        CancellationToken ct)
    {
        var review = await sender.Send(command with { Id = id }, ct);

        Response.Headers.ETag = ETagHelper.Format(review.Version);

        return Ok(review);
    }

    [HttpPost("reviews/{id}/admin-response")]
    public async Task<ActionResult<ReviewDto>> AddAdminResponse(
        string id,
        [FromBody] AddAdminResponseCommand command,
        CancellationToken ct)
    {
        var expectedVersion = ETagHelper.Parse(Request.Headers.IfMatch.FirstOrDefault()) ?? command.ExpectedVersion;

        var review = await sender.Send(command with { Id = id, ExpectedVersion = expectedVersion }, ct);

        Response.Headers.ETag = ETagHelper.Format(review.Version);

        return Ok(review);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ModerationLogPageDto>> Logs(
        [FromQuery] string? reviewId,
        [FromQuery] int limit = 20,
        [FromQuery] string? cursor = null,
        CancellationToken ct = default)
    {
        return Ok(await sender.Send(new GetModerationLogsQuery(reviewId, limit, cursor), ct));
    }
}
