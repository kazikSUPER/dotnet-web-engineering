using Orders.BLL.DTOs.Payments;

namespace Orders.BLL.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<PaymentResponseDto> ProcessAsync(ProcessPaymentRequest request, CancellationToken ct = default);
}
