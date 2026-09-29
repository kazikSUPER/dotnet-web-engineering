namespace Bakery.Domain.Entities;

public class Payment
{
    public int OrderId { get; set; }
    public int Id { get => OrderId; set => OrderId = value; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string TransactionReference { get => TransactionNumber; set => TransactionNumber = value; }
    public string PaymentMethod { get; set; } = "CreditCard";
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }
}
