using Bakery.BLL.DTOs.Orders;

namespace Bakery.BLL.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResponse<OrderResponseDto>> GetByCustomerIdAsync(int customerId, OrderQuery query, CancellationToken ct = default);
    Task<OrderResponseDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}
