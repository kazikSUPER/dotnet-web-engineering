using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs.Payments;

public sealed class ProcessPaymentRequest
{
    [Range(1, int.MaxValue)] public int OrderId { get; init; }
    [Required, RegularExpression("^(CreditCard|ApplePay|GooglePay|Cash)$")] public string PaymentMethod { get; init; } = "CreditCard";
    [Range(0.01, 100000)] public decimal Amount { get; init; }
}
