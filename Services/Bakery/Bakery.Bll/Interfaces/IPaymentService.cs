using Bakery.BLL.DTOs;

namespace Bakery.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto, CancellationToken ct = default);
}
