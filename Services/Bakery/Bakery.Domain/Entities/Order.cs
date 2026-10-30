using Bakery.Domain.Exceptions;

namespace Bakery.Domain.Entities;

public class Order
{
    private Order() { }

    public Order(int customerId, string orderNumber, string? notes = null)
    {
        if (customerId <= 0) throw new DomainValidationException("Некоректний ідентифікатор клієнта.");
        CustomerId = customerId;
        OrderNumber = orderNumber;
        Status = "Pending";
        Notes = notes;
        OrderDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    public string Status { get; private set; } = "Pending";
    public decimal TotalAmount { get; private set; }
    public DateTime OrderDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "Customer";
    public bool IsDeleted { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public Customer? Customer { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public Payment? Payment { get; private set; }
    public ICollection<OrderStatusHistory> StatusHistory { get; private set; } = new List<OrderStatusHistory>();

    public void AddItem(Product product, int quantity)
    {
        if (quantity <= 0) throw new DomainValidationException("Кількість має бути більшою за нуль.");
        product.RemoveStock(quantity);
        Items.Add(new OrderItem(product.Id, product.Name, product.Price, quantity));
        RecalculateTotal();
    }

    public void RecalculateTotal() => TotalAmount = Items.Sum(x => x.TotalPrice);

    public void ChangeStatus(string newStatus)
    {
        var allowed = Status switch
        {
            "Pending" => new[] { "Paid", "Cancelled" },
            "Paid" => new[] { "Baking", "Cancelled" },
            "Baking" => new[] { "Ready" },
            "Ready" => new[] { "Delivered" },
            _ => Array.Empty<string>()
        };
        if (!allowed.Contains(newStatus)) throw new BusinessConflictException($"Перехід зі статусу {Status} у {newStatus} заборонений.");
        Status = newStatus;
        StatusHistory.Add(new OrderStatusHistory(Id, newStatus, DateTime.UtcNow, $"Статус змінено на {newStatus}"));
    }
}
