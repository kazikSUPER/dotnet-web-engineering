namespace Bakery.BLL.DTOs.Orders;

public sealed class OrderQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Status { get; init; }
    public string? SortBy { get; init; } = "orderDate";
    public bool Desc { get; init; } = true;
}
