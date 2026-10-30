using Bakery.DAL.Interfaces;
using Bakery.DAL.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bakery.DAL.Data;

public class UnitOfWork(BakeryDbContext context) : IUnitOfWork
{
    private ICustomerRepository? customers;
    private IOrderRepository? orders;
    private IPaymentRepository? payments;
    private IProductRepository? products;

    public ICustomerRepository Customers => customers ??= new CustomerRepository(context);
    public IOrderRepository Orders => orders ??= new OrderRepository(context);
    public IPaymentRepository Payments => payments ??= new PaymentRepository(context);
    public IProductRepository Products => products ??= new ProductRepository(context);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default) => await context.Database.BeginTransactionAsync(ct);
}
