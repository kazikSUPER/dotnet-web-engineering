using Bakery.BLL.DTOs.Payments;
using Bakery.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
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
