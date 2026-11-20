using Orders.BLL.DTOs.Orders;
using Orders.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    [RequirePermission("orders.read")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:int}")]
    [RequirePermission("orders.read")]
    public async Task<ActionResult<PagedResponse<OrderResponseDto>>> GetByCustomer(int customerId, [FromQuery] OrderQuery query, CancellationToken ct) => Ok(await service.GetByCustomerIdAsync(customerId, query, ct));

    [HttpGet("{id:int}/history")]
    [RequirePermission("orders.read")]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryDto>>> GetStatusHistory(int id, CancellationToken ct) => Ok(await service.GetStatusHistoryAsync(id, ct));

    [HttpPost]
    [RequirePermission("orders.write")]
    public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id:int}/status")]
    [RequirePermission("orders.write")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await service.UpdateStatusAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [RequirePermission("orders.cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }
}
