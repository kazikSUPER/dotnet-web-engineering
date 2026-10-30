using Bakery.BLL.DTOs.Payments;

namespace Bakery.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<PaymentResponseDto> ProcessAsync(ProcessPaymentRequest request, CancellationToken ct = default);
}
