namespace Platform.EventBus.Events.Contracts;

[EventRoutingKey("catalog.product.created")]
public sealed record ProductCreatedEvent(
    int ProductId,
    string Name,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);

[EventRoutingKey("catalog.product.renamed")]
public sealed record ProductRenamedEvent(
    int ProductId,
    string OldName,
    string NewName,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);

[EventRoutingKey("catalog.product.price-changed")]
public sealed record ProductPriceChangedEvent(
    int ProductId,
    decimal OldPrice,
    decimal NewPrice,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);

[EventRoutingKey("catalog.category.updated")]
public sealed record CategoryUpdatedEvent(
    int CategoryId,
    string Name,
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default) : IntegrationEvent(EventId, CorrelationId, OccurredAt);
