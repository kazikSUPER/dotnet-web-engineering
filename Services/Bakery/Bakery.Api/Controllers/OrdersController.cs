using Bakery.BLL.DTOs.Orders;
using Bakery.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(IOrderService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<PagedResponse<OrderResponseDto>>> GetByCustomer(int customerId, [FromQuery] OrderQuery query, CancellationToken ct) => Ok(await service.GetByCustomerIdAsync(customerId, query, ct));

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        await service.UpdateStatusAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }
}
