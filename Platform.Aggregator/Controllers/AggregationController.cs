using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregationController(
    OrderAssemblyService orderAssembly,
    ProductOverviewService productOverview,
    ILogger<AggregationController> logger) : ControllerBase
{
    [HttpGet("orders/{orderId:int}/full")]
    [ProducesResponseType(typeof(OrderAggregateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OrderAggregateDto>> GetOrderFull(int orderId, CancellationToken ct)
    {
        try
        {
            var aggregate = await orderAssembly.AssembleAsync(orderId, ct);

            if (aggregate is null)
            {
                return NotFound(Problem("Замовлення не знайдено", $"Сервіс замовлень не повернув замовлення {orderId}.", StatusCodes.Status404NotFound));
            }

            return Ok(aggregate);
        }
        catch (DownstreamServiceException exception)
        {
            return Unavailable(exception);
        }
    }

    [HttpGet("products/{productId:int}/overview")]
    [ProducesResponseType(typeof(ProductOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ProductOverviewDto>> GetProductOverview(int productId, CancellationToken ct)
    {
        try
        {
            var overview = await productOverview.BuildAsync(productId, ct);

            if (overview is null)
            {
                return NotFound(Problem("Товар не знайдено", $"Сервіс каталогу не повернув товар {productId}.", StatusCodes.Status404NotFound));
            }

            return Ok(overview);
        }
        catch (DownstreamServiceException exception)
        {
            return Unavailable(exception);
        }
    }

    private ObjectResult Unavailable(DownstreamServiceException exception)
    {
        logger.LogWarning(
            exception,
            "Агрегування перерване: критичний сервіс {ServiceName} недоступний на {Path} ({Reason})",
            exception.ServiceName,
            exception.Path,
            exception.Reason);

        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            Problem(
                "Сервіс недоступний",
                $"Не вдалося отримати обов'язкові дані від сервісу «{exception.ServiceName}»: {exception.Reason}.",
                StatusCodes.Status503ServiceUnavailable));
    }

    private ProblemDetails Problem(string title, string detail, int status) => new()
    {
        Title = title,
        Detail = detail,
        Status = status,
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5",
        Extensions =
        {
            ["correlationId"] = CorrelationIdMiddleware.Of(HttpContext),
            ["traceId"] = HttpContext.TraceIdentifier,
        },
    };
}
