using Bakery.Domain.Entities;
using Bakery.Domain.Interfaces;

namespace Bakery.DAL.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken ct = default);
    Task<bool> UpdateStatusViaProcedureAsync(int orderId, string newStatus, string updatedBy, CancellationToken ct = default);
    Task AddOrderItemAsync(OrderItem item, CancellationToken ct = default);
}
