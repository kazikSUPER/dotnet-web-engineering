namespace Bakery.DAL.Interfaces;

public interface IUnitOfWork
{
    ICustomerRepository Customers { get; }
    IOrderRepository Orders { get; }
    IPaymentRepository Payments { get; }
    IProductRepository Products { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default);
}
