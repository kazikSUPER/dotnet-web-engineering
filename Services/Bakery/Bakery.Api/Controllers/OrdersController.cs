using Bakery.BLL.DTOs;
using Bakery.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id, CancellationToken ct = default)
    {
        var order = await _orderService.GetByIdAsync(id, ct);
        if (order == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Замовлення не знайдено",
                Detail = $"Замовлення з Id {id} не знайдено.",
                Status = StatusCodes.Status404NotFound
            });
        }
        return Ok(order);
    }

    [HttpGet("customer/{customerId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponseDto>>> GetByCustomer(int customerId, CancellationToken ct = default)
    {
        var orders = await _orderService.GetByCustomerIdAsync(customerId, ct);
        return Ok(orders);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> Create([FromBody] CreateOrderDto dto, CancellationToken ct = default)
    {
        var created = await _orderService.CreateOrderAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct = default)
    {
        await _orderService.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct = default)
    {
        await _orderService.CancelOrderAsync(id, ct);
        return NoContent();
    }
}
