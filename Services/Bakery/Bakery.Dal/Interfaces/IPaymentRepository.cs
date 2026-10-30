using Bakery.Domain.Entities;

namespace Bakery.DAL.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
}
