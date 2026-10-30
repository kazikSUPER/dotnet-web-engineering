using Bakery.Domain.Exceptions;

namespace Bakery.Domain.Entities;

public class Payment
{
    private Payment() { }

    public Payment(int orderId, string transactionNumber, string paymentMethod, decimal amount)
    {
        if (orderId <= 0 || string.IsNullOrWhiteSpace(transactionNumber) || amount <= 0) throw new DomainValidationException("Некоректні дані оплати.");
        OrderId = orderId;
        TransactionNumber = transactionNumber;
        PaymentMethod = paymentMethod;
        Amount = amount;
        PaymentDate = DateTime.UtcNow;
        Status = "Completed";
    }

    public int OrderId { get; private set; }
    public string TransactionNumber { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public string Status { get; private set; } = "Completed";
    public byte[]? RowVersion { get; private set; }
    public Order? Order { get; private set; }
}
