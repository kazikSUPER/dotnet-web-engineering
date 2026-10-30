namespace Bakery.Domain.Entities;

public class OrderStatusHistory
{
    private OrderStatusHistory() { }

    public OrderStatusHistory(int orderId, string status, DateTime changedAt, string? comment)
    {
        OrderId = orderId;
        Status = status;
        ChangedAt = changedAt;
        Comment = comment;
    }

    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime ChangedAt { get; private set; }
    public string? Comment { get; private set; }
    public Order? Order { get; private set; }
}
