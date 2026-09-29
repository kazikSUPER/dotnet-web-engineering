using Bakery.BLL.DTOs;

namespace Bakery.BLL.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderResponseDto>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(int id, CancellationToken ct = default);
}
