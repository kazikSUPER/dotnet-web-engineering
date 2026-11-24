using System.Diagnostics.Metrics;

namespace Bakery.Reviews.Application.Caching;

public class CacheMetrics
{
    public const string MeterName = "Platform.Caching";

    private static Meter _meter = new(MeterName, "1.0");
    private static Counter<long> _hits = _meter.CreateCounter<long>("cache.hits", "count", "Number of cache reads served from L1 or L2");
    private static Counter<long> _misses = _meter.CreateCounter<long>("cache.misses", "count", "Number of cache reads that fell through to the source");
    private static Counter<long> _failures = _meter.CreateCounter<long>("cache.redis.failures", "count", "Number of Redis cache failures resulting in graceful fallback");

    public static Meter Meter => _meter;
    public static Counter<long> Hits => _hits;
    public static Counter<long> Misses => _misses;
    public static Counter<long> Failures => _failures;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName, "1.0");
        _hits = _meter.CreateCounter<long>("cache.hits", "count", "Number of cache reads served from L1 or L2");
        _misses = _meter.CreateCounter<long>("cache.misses", "count", "Number of cache reads that fell through to the source");
        _failures = _meter.CreateCounter<long>("cache.redis.failures", "count", "Number of Redis cache failures resulting in graceful fallback");
    }

    public static void RecordHit(string level, string cache) =>
        _hits.Add(1, new KeyValuePair<string, object?>("level", level), new("cache", cache));

    public static void RecordMiss(string level, string cache) =>
        _misses.Add(1, new KeyValuePair<string, object?>("level", level), new("cache", cache));

    public static void RecordFailure(string operation, string cache) =>
        _failures.Add(1, new KeyValuePair<string, object?>("operation", operation), new("cache", cache));
}
