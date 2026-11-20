namespace Platform.EventBus.Events.Contracts;

[EventRoutingKey("catalog.product.created")]
public sealed record ProductCreatedEvent(
    int ProductId,
    string Name,
    decimal Price,
    int Stock,
    int CategoryId,
    string CategoryName) : IntegrationEvent;

[EventRoutingKey("catalog.product.renamed")]
public sealed record ProductRenamedEvent(
    int ProductId,
    string OldName,
    string NewName) : IntegrationEvent;

[EventRoutingKey("catalog.product.price-changed")]
public sealed record ProductPriceChangedEvent(
    int ProductId,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;

[EventRoutingKey("catalog.category.updated")]
public sealed record CategoryUpdatedEvent(
    int CategoryId,
    string Name) : IntegrationEvent;
