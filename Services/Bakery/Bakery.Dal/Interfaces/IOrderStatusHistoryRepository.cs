using Bakery.Domain.Entities;
using Bakery.Domain.Interfaces;

namespace Bakery.DAL.Interfaces;

public interface IOrderStatusHistoryRepository : IGenericRepository<OrderStatusHistory>
{
    Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
}
