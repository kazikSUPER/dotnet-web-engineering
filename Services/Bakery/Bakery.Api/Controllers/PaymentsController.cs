using Bakery.BLL.DTOs;
using Bakery.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bakery.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("order/{orderId:int}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetByOrderId(int orderId, CancellationToken ct = default)
    {
        var payment = await _paymentService.GetByOrderIdAsync(orderId, ct);
        if (payment == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Оплату не знайдено",
                Detail = $"Оплату для замовлення #{orderId} не знайдено.",
                Status = StatusCodes.Status404NotFound
            });
        }
        return Ok(payment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> ProcessPayment([FromBody] ProcessPaymentDto dto, CancellationToken ct = default)
    {
        var payment = await _paymentService.ProcessPaymentAsync(dto, ct);
        return CreatedAtAction(nameof(GetByOrderId), new { orderId = payment.OrderId }, payment);
    }
}
