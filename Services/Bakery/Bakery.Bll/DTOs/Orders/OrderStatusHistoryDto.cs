namespace Bakery.BLL.DTOs.Orders;

public sealed class OrderStatusHistoryDto
{
    public int Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? Comment { get; init; }
}
