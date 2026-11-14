using System.Diagnostics.Metrics;

namespace Bakery.Reviews.Application.Caching;

public static class CacheMetrics
{
    public const string MeterName = "Platform.Caching";

    public static readonly Meter Meter = new(MeterName, "1.0");

    public static readonly Counter<long> Hits = Meter.CreateCounter<long>(
        "cache.hits", "count", "Number of cache reads served from L1 or L2");

    public static readonly Counter<long> Misses = Meter.CreateCounter<long>(
        "cache.misses", "count", "Number of cache reads that fell through to the source");

    public static void RecordHit(string level, string cache) =>
        Hits.Add(1, new KeyValuePair<string, object?>("level", level), new("cache", cache));

    public static void RecordMiss(string level, string cache) =>
        Misses.Add(1, new KeyValuePair<string, object?>("level", level), new("cache", cache));
}
