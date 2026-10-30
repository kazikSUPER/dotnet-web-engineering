namespace Bakery.BLL.DTOs.Orders;

public sealed class OrderResponseDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OrderDate { get; init; }
    public string? Notes { get; init; }
    public List<OrderItemResponseDto> Items { get; init; } = [];
}

public sealed class OrderItemResponseDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
}

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
