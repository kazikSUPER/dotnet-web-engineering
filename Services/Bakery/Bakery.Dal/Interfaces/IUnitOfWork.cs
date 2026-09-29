using System.Data;

namespace Bakery.DAL.Interfaces;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }

    Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
