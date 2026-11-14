using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace Platform.Aggregator.Caching;

public static class AggregateCacheMetrics
{
    public static readonly Meter Meter = new("Platform.Caching", "1.0");
    public static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits", "count");
    public static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses", "count");
}

public interface IAggregatedCache
{
    Task<T?> TryGetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
}

public sealed class AggregatedCache(
    IDistributedCache redis,
    ILogger<AggregatedCache> logger) : IAggregatedCache
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);

    public static string OrderFull(int orderId) => $"aggregate:order:{orderId}:full";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<T?> TryGetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await redis.GetAsync(key, ct);
            if (bytes is null)
            {
                AggregateCacheMetrics.Misses.Add(1, new KeyValuePair<string, object?>("level", "L2"), new("cache", "aggregate"));
                logger.LogInformation("CacheMiss key={Key} (aggregate)", key);
                return default;
            }

            AggregateCacheMetrics.Hits.Add(1, new KeyValuePair<string, object?>("level", "L2"), new("cache", "aggregate"));
            logger.LogInformation("CacheHit level=L2 key={Key} (aggregate)", key);
            return JsonSerializer.Deserialize<T>(bytes, Json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis read failed key={Key}, recomputing aggregate", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
            await redis.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Redis write failed key={Key}", key);
        }
    }
}
