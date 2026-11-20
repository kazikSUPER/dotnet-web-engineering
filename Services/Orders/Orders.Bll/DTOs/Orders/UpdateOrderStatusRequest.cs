using System.ComponentModel.DataAnnotations;

namespace Orders.BLL.DTOs.Orders;

public sealed class UpdateOrderStatusRequest
{
    [Required, RegularExpression("^(Pending|Paid|Baking|Ready|Delivered|Cancelled)$")] public string NewStatus { get; init; } = string.Empty;
}
