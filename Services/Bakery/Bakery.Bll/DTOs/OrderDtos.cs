using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateOrderDto
{
    [Required(ErrorMessage = "Ідентифікатор клієнта є обов'язковим")]
    [Range(1, int.MaxValue, ErrorMessage = "Некоректний CustomerId")]
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Замовлення повинно містити щонайменше одну позицію")]
    [MinLength(1, ErrorMessage = "Замовлення не може бути порожнім")]
    public List<CreateOrderItemDto> Items { get; set; } = new();

    [StringLength(250, ErrorMessage = "Примітки не можуть перевищувати 250 символів")]
    public string? Notes { get; set; }
}

public class CreateOrderItemDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Некоректний ProductId")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Кількість повинна бути від 1 до 100 одиниць")]
    public int Quantity { get; set; } = 1;
}

public class UpdateOrderStatusDto
{
    [Required(ErrorMessage = "Новий статус є обов'язковим")]
    [RegularExpression("^(Pending|Paid|Baking|Ready|Delivered|Cancelled)$", 
        ErrorMessage = "Допустимі статуси: Pending, Paid, Baking, Ready, Delivered, Cancelled")]
    public string NewStatus { get; set; } = string.Empty;

    [StringLength(50)]
    public string UpdatedBy { get; set; } = "Dispatcher";
}
