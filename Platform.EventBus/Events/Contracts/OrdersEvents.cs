namespace Platform.EventBus.Events.Contracts;

[EventRoutingKey("orders.order.placed")]
public sealed record OrderPlacedEvent(
    int OrderId,
    string OrderNumber,
    int CustomerId,
    decimal TotalAmount,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);

[EventRoutingKey("orders.order.status-changed")]
public sealed record OrderStatusChangedEvent(
    int OrderId,
    string OldStatus,
    string NewStatus,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);

[EventRoutingKey("orders.payment.processed")]
public sealed record PaymentProcessedEvent(
    string TransactionNumber,
    int OrderId,
    decimal Amount,
    string Status,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);
