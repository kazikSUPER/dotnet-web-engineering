using Catalog.DAL.Interfaces;
using Catalog.DAL.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Catalog.DAL.Data;

public class UnitOfWork(CatalogDbContext context) : IUnitOfWork
{
    private IProductRepository? products;

    public IProductRepository Products => products ??= new ProductRepository(context);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default) => await context.Database.BeginTransactionAsync(ct);
}
