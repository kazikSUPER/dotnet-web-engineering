using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs.Orders;

public sealed class CreateOrderRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; init; }
    [Required, MinLength(1)] public List<CreateOrderItemRequest> Items { get; init; } = [];
    [StringLength(250)] public string? Notes { get; init; }
}

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; init; }
    [Range(1, 100)] public int Quantity { get; init; } = 1;
}
