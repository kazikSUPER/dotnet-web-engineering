using System.Diagnostics.Metrics;

namespace Platform.Aggregator.Metrics;

public sealed class AggregatorMetrics
{
    private readonly Meter _meter;
    private readonly Histogram<double> _aggregateDuration;
    private readonly Counter<long> _upstreamFailures;

    public AggregatorMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Platform.Aggregator");
        _aggregateDuration = _meter.CreateHistogram<double>("aggregate.duration", "ms", "Aggregation duration in milliseconds");
        _upstreamFailures = _meter.CreateCounter<long>("aggregate.upstream.failures", "count", "Count of upstream service failures");
    }

    public void RecordDuration(double milliseconds, string status)
    {
        _aggregateDuration.Record(milliseconds, 
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("result", status));
    }

    public void RecordUpstreamFailure(string service)
    {
        _upstreamFailures.Add(1, new KeyValuePair<string, object?>("service", service));
    }
}
