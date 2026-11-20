namespace Catalog.BLL.Caching;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan l1Ttl,
        TimeSpan l2Ttl,
        CancellationToken ct = default);

    Task RemoveAsync(string key, string reason, CancellationToken ct = default);
}
