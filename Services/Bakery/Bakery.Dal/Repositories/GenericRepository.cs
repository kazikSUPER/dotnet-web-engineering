using Bakery.DAL.Data;
using Bakery.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bakery.DAL.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly BakeryDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(BakeryDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default) => await Set.FindAsync([id], ct);
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default) => await Set.AsNoTracking().ToListAsync(ct);
    public virtual async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public virtual void Update(T entity) => Set.Update(entity);
    public virtual void Delete(T entity) => Set.Remove(entity);
    public virtual Task<int> CountAsync(CancellationToken ct = default) => Set.CountAsync(ct);
    public virtual Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct = default) => SpecificationEvaluator.GetQuery(Set.AsNoTracking(), specification, applyPaging: false).CountAsync(ct);
    public virtual async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct = default) => await SpecificationEvaluator.GetQuery(Set.AsNoTracking(), specification).ToListAsync(ct);
}
