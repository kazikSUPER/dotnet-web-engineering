using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Catalog.Api.IntegrationEvents.Consumers;

public sealed class CatalogReviewEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogReviewEventsConsumer> logger)
    : RabbitMqConsumerBackgroundService(
        connection,
        scopeFactory,
        EventBusTopology.CatalogReviewEventsQueue,
        logger)
{
    protected override Type? ResolveEventType(string eventTypeName) => eventTypeName switch
    {
        nameof(ReviewCreatedEvent) => typeof(ReviewCreatedEvent),
        nameof(ReviewModeratedEvent) => typeof(ReviewModeratedEvent),
        _ => null
    };
}
