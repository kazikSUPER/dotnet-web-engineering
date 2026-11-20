using Orders.Domain.Entities;

namespace Orders.DAL.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<Order?> GetWithExplicitPaymentAsync(int id, CancellationToken ct = default);
    Task<Order?> GetWithExplicitStatusHistoryAsync(int id, CancellationToken ct = default);
}
