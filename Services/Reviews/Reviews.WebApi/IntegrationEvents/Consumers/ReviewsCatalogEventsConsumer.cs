using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Reviews.WebApi.IntegrationEvents.Consumers;

public sealed class ReviewsCatalogEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<ReviewsCatalogEventsConsumer> logger)
    : RabbitMqConsumerBackgroundService(
        connection,
        scopeFactory,
        EventBusTopology.ReviewsCatalogEventsQueue,
        logger)
{
    protected override Type? ResolveEventType(string eventTypeName) => eventTypeName switch
    {
        nameof(ProductRenamedEvent) => typeof(ProductRenamedEvent),
        _ => null
    };
}
