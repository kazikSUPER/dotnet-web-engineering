namespace Catalog.DAL.Interfaces;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default);
}
