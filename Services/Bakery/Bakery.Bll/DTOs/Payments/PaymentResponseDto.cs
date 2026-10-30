namespace Bakery.BLL.DTOs.Payments;

public sealed class PaymentResponseDto
{
    public int OrderId { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime PaymentDate { get; init; }
    public string Status { get; init; } = string.Empty;
}
