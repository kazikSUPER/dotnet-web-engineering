using Orders.DAL.Interfaces;
using Orders.DAL.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Orders.DAL.Data;

public class UnitOfWork(OrdersDbContext context) : IUnitOfWork
{
    private ICustomerRepository? customers;
    private IOrderRepository? orders;
    private IPaymentRepository? payments;

    public ICustomerRepository Customers => customers ??= new CustomerRepository(context);
    public IOrderRepository Orders => orders ??= new OrderRepository(context);
    public IPaymentRepository Payments => payments ??= new PaymentRepository(context);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default) => await context.Database.BeginTransactionAsync(ct);
    public Task CommitTransactionAsync(CancellationToken ct = default) => context.Database.CommitTransactionAsync(ct);
    public Task RollbackTransactionAsync(CancellationToken ct = default) => context.Database.RollbackTransactionAsync(ct);
}
