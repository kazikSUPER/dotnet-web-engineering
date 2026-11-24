using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Catalog.BLL.Caching;

public sealed class TwoLevelCacheService(
    IMemoryCache l1,
    IDistributedCache l2,
    ILogger<TwoLevelCacheService> logger) : ICacheService
{
    private const string CacheName = "catalog";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan l1Ttl,
        TimeSpan l2Ttl,
        CancellationToken ct = default)
    {
        if (l1.TryGetValue(key, out T? cached) && cached is not null)
        {
            CacheMetrics.RecordHit("L1", CacheName);
            logger.LogInformation("CacheHit level=L1 key={Key}", key);
            return cached;
        }

        T? fromL2 = await TryReadL2Async<T>(key, ct);
        if (fromL2 is not null)
        {
            SetL1(key, fromL2, l1Ttl);
            CacheMetrics.RecordHit("L2", CacheName);
            logger.LogInformation("CacheHit level=L2 key={Key} (promoted to L1)", key);
            return fromL2;
        }

        CacheMetrics.RecordMiss("L2", CacheName);
        logger.LogInformation("CacheMiss key={Key} loading from source", key);

        T value = await factory(ct);

        await WriteL2Async(key, value, l2Ttl, ct);
        SetL1(key, value, l1Ttl);
        return value;
    }

    public async Task RemoveAsync(string key, string reason, CancellationToken ct = default)
    {
        l1.Remove(key);
        try
        {
            await l2.RemoveAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheMetrics.RecordFailure("remove", CacheName);
            logger.LogWarning(ex, "Redis remove failed key={Key}", key);
        }

        logger.LogInformation("CacheInvalidated key={Key} reason={Reason}", key, reason);
    }

    private void SetL1<T>(string key, T value, TimeSpan ttl) =>
        l1.Set(key, value, new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = TimeSpan.FromSeconds(30),
            AbsoluteExpirationRelativeToNow = ttl,
        }.SetSize(1));

    private async Task<T?> TryReadL2Async<T>(string key, CancellationToken ct)
    {
        try
        {
            byte[]? bytes = await l2.GetAsync(key, ct);
            return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, Json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheMetrics.RecordFailure("read", CacheName);
            logger.LogWarning(ex, "Redis read failed key={Key}, falling back to source", key);
            return default;
        }
    }

    private async Task WriteL2Async<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
            await l2.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheMetrics.RecordFailure("write", CacheName);
            logger.LogWarning(ex, "Redis write failed key={Key}", key);
        }
    }
}
