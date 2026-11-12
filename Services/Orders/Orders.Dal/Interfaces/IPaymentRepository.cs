using Orders.Domain.Entities;

namespace Orders.DAL.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
}
