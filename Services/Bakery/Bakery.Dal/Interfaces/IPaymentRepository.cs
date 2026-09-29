using Bakery.Domain.Entities;
using Bakery.Domain.Interfaces;

namespace Bakery.DAL.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(int orderId, CancellationToken ct = default);
    Task<Payment?> GetByTransactionNumberAsync(string transactionNumber, CancellationToken ct = default);
    Task<bool> ProcessPaymentViaProcedureAsync(int orderId, string transactionNumber, string paymentMethod, decimal amount, string processedBy, CancellationToken ct = default);
}
