using System.Diagnostics.Metrics;

namespace Catalog.Api.Metrics;

public sealed class CatalogMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _productsViewed;
    private readonly Histogram<double> _productsDuration;
    private int _availableProductsCount;

    public CatalogMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Catalog.Api");
        _productsViewed = _meter.CreateCounter<long>("products.viewed", "count", "Number of product views");
        _productsDuration = _meter.CreateHistogram<double>("products.duration", "ms", "Duration of catalog operations in milliseconds");

        _meter.CreateObservableGauge<int>(
            "products.available",
            () => _availableProductsCount,
            "count",
            "Number of available products");
    }

    public void ProductViewed(string category)
    {
        _productsViewed.Add(1, new KeyValuePair<string, object?>("category", category));
    }

    public void RecordDuration(double milliseconds, string operation, string status)
    {
        _productsDuration.Record(milliseconds, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", status));
    }

    public void SetAvailableProductsCount(int count)
    {
        Interlocked.Exchange(ref _availableProductsCount, count);
    }
}
