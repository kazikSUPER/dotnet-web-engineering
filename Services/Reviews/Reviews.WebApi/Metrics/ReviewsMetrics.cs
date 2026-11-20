using System.Diagnostics.Metrics;

namespace Reviews.WebApi.Metrics;

public sealed class ReviewsMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _reviewsCreated;
    private readonly Counter<long> _reviewsModerated;
    private readonly Histogram<double> _operationDuration;
    private int _pendingModerationCount;

    public ReviewsMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Reviews.WebApi");
        _reviewsCreated = _meter.CreateCounter<long>("reviews.created", "count", "Number of reviews created");
        _reviewsModerated = _meter.CreateCounter<long>("reviews.moderated", "count", "Number of reviews moderated");
        _operationDuration = _meter.CreateHistogram<double>("reviews.operation.duration", "ms", "Duration of review operations in milliseconds");

        _meter.CreateObservableGauge<int>(
            "reviews.moderation.pending",
            () => _pendingModerationCount,
            "count",
            "Pending moderation queue size");
    }

    public void ReviewCreated(string status)
    {
        _reviewsCreated.Add(1, new KeyValuePair<string, object?>("status", status));
    }

    public void ReviewModerated(string action)
    {
        _reviewsModerated.Add(1, new KeyValuePair<string, object?>("action", action));
    }

    public void RecordDuration(double milliseconds, string operation, string status)
    {
        _operationDuration.Record(milliseconds, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", status));
    }

    public void SetPendingModerationCount(int count)
    {
        Interlocked.Exchange(ref _pendingModerationCount, count);
    }
}
