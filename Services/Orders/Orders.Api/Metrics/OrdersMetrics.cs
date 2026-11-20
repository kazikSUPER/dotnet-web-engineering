using System.Diagnostics.Metrics;

namespace Orders.Api.Metrics;

public sealed class OrdersMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _ordersCreated;
    private readonly Histogram<double> _orderDuration;
    private readonly Counter<long> _paymentsProcessed;
    private int _pendingOrdersCount;

    public OrdersMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Orders.Api");
        _ordersCreated = _meter.CreateCounter<long>("orders.created", "count", "Number of orders created");
        _orderDuration = _meter.CreateHistogram<double>("orders.duration", "ms", "Duration of order operations in milliseconds");
        _paymentsProcessed = _meter.CreateCounter<long>("payments.processed", "count", "Number of payments processed");

        _meter.CreateObservableGauge<int>(
            "orders.pending",
            () => _pendingOrdersCount,
            "count",
            "Current number of pending orders");
    }

    public void OrderCreated(string status, string paymentMethod)
    {
        _ordersCreated.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("payment_method", paymentMethod));
    }

    public void RecordDuration(double milliseconds, string operation, string status)
    {
        _orderDuration.Record(milliseconds, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("status", status));
    }

    public void PaymentProcessed(string method, string result)
    {
        _paymentsProcessed.Add(1, new KeyValuePair<string, object?>("method", method), new KeyValuePair<string, object?>("result", result));
    }

    public void SetPendingOrdersCount(int count)
    {
        Interlocked.Exchange(ref _pendingOrdersCount, count);
    }
}
