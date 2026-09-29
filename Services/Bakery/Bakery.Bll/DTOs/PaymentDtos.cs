using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs;

public class PaymentDto
{
    public int OrderId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProcessPaymentDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Некоректний OrderId")]
    public int OrderId { get; set; }

    [Required]
    [RegularExpression("^(CreditCard|ApplePay|GooglePay|Cash)$", ErrorMessage = "Підтримуються: CreditCard, ApplePay, GooglePay, Cash")]
    public string PaymentMethod { get; set; } = "CreditCard";

    [Required]
    [Range(0.01, 100000.00, ErrorMessage = "Сума повинна бути більшою за 0")]
    public decimal Amount { get; set; }
}
