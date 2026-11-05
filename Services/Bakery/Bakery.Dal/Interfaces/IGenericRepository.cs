namespace Bakery.DAL.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
    Task<int> CountAsync(CancellationToken ct = default);
    IQueryable<T> WithSpecification(ISpecification<T> specification, bool applyPaging = true);
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> specification, CancellationToken ct = default);
}
