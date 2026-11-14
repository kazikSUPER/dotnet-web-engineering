using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator/grpc")]
public sealed class GrpcAggregationController(GrpcOrderAggregateService service) : ControllerBase
{
    [HttpGet("orders/{orderId:int}/full")]
    public async Task<ActionResult<GrpcOrderAggregateDto>> GetOrderFull(int orderId, CancellationToken ct)
    {
        try
        {
            var result = await service.GetOrderFullAsync(orderId, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (GrpcDownstreamUnavailableException exception)
        {
            return Problem(
                title: "gRPC downstream service unavailable",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
