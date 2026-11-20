using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private static readonly List<PaymentRecord> InMemoryPayments =
    [
        new("pay_1", 101, 150.00m, "Completed", DateTime.UtcNow.AddHours(-2)),
        new("pay_2", 102, 320.50m, "Completed", DateTime.UtcNow.AddHours(-1))
    ];

    [HttpGet]
    [RequirePermission("payments.read")]
    public IActionResult GetAll()
    {
        return Ok(InMemoryPayments);
    }

    [HttpGet("{id}")]
    [RequirePermission("payments.read")]
    public IActionResult GetById(string id)
    {
        var item = InMemoryPayments.FirstOrDefault(p => p.Id == id);
        return item is not null ? Ok(item) : NotFound();
    }

    [HttpPost]
    [RequirePermission("payments.write")]
    public IActionResult Create([FromBody] CreatePaymentDto dto)
    {
        var payment = new PaymentRecord($"pay_{Guid.NewGuid():N}"[..8], dto.OrderId, dto.Amount, "Completed", DateTime.UtcNow);
        InMemoryPayments.Add(payment);
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpPost("{id}/refund")]
    [RequirePermission("payments.write")]
    public IActionResult Refund(string id)
    {
        var item = InMemoryPayments.FirstOrDefault(p => p.Id == id);
        if (item is null) return NotFound();

        return Ok(new { message = $"Платіж {id} успішно повернено (refund)." });
    }
}

public record PaymentRecord(string Id, int OrderId, decimal Amount, string Status, DateTime CreatedAt);
public record CreatePaymentDto(int OrderId, decimal Amount);
