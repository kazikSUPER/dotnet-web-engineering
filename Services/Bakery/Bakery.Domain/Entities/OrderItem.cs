using Bakery.Domain.Exceptions;

namespace Bakery.Domain.Entities;

public class OrderItem
{
    private OrderItem() { }

    public OrderItem(int productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId <= 0 || quantity <= 0 || unitPrice <= 0) throw new DomainValidationException("Некоректні дані позиції замовлення.");
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Order? Order { get; private set; }
    public Product? Product { get; private set; }
}
