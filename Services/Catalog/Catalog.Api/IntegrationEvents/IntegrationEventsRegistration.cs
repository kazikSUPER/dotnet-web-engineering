using Catalog.Api.IntegrationEvents.Consumers;
using Catalog.Api.IntegrationEvents.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Catalog.Api.IntegrationEvents;

public static class IntegrationEventsRegistration
{
    public static IServiceCollection AddCatalogIntegrationEvents(this IServiceCollection services)
    {
        services.AddEventBus();

        services.AddScoped<IIntegrationEventHandler<ReviewCreatedEvent>, ReviewCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ReviewModeratedEvent>, ReviewModeratedEventHandler>();

        services.AddHostedService<CatalogReviewEventsConsumer>();

        return services;
    }
}
