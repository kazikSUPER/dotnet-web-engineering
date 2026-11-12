using Orders.BLL.DTOs.Payments;
using Orders.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService service) : ControllerBase
{
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<PaymentResponseDto>> GetByOrderId(int orderId, CancellationToken ct)
    {
        var result = await service.GetByOrderIdAsync(orderId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> Process(ProcessPaymentRequest request, CancellationToken ct)
    {
        var result = await service.ProcessAsync(request, ct);
        return CreatedAtAction(nameof(GetByOrderId), new { orderId = result.OrderId }, result);
    }
}
