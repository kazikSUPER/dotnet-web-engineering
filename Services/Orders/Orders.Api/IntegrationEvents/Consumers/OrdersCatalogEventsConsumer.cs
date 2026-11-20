using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Orders.Api.IntegrationEvents.Consumers;

public sealed class OrdersCatalogEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrdersCatalogEventsConsumer> logger)
    : RabbitMqConsumerBackgroundService(
        connection,
        scopeFactory,
        EventBusTopology.OrdersCatalogEventsQueue,
        logger)
{
    protected override Type? ResolveEventType(string eventTypeName) => eventTypeName switch
    {
        nameof(ProductCreatedEvent) => typeof(ProductCreatedEvent),
        nameof(ProductRenamedEvent) => typeof(ProductRenamedEvent),
        nameof(ProductPriceChangedEvent) => typeof(ProductPriceChangedEvent),
        nameof(CategoryUpdatedEvent) => typeof(CategoryUpdatedEvent),
        _ => null
    };
}
