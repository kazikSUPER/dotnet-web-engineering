using Platform.Aggregator.Contracts;

namespace Platform.Aggregator.Clients;

public sealed class OrdersClient(HttpClient http, ILogger<OrdersClient> logger)
    : DownstreamClient(http, logger, "orders-api")
{
    public Task<OrderContract?> GetOrderAsync(int orderId, CancellationToken ct) =>
        GetAsync<OrderContract>($"api/orders/{orderId}", ct);

    public Task<CustomerContract?> GetCustomerAsync(int customerId, CancellationToken ct) =>
        GetAsync<CustomerContract>($"api/customers/{customerId}", ct);

    public Task<PaymentContract?> GetPaymentAsync(int orderId, CancellationToken ct) =>
        GetAsync<PaymentContract>($"api/payments/order/{orderId}", ct);

    public Task<OrderStatusHistoryContract[]?> GetStatusHistoryAsync(int orderId, CancellationToken ct) =>
        GetAsync<OrderStatusHistoryContract[]>($"api/orders/{orderId}/history", ct);
}
