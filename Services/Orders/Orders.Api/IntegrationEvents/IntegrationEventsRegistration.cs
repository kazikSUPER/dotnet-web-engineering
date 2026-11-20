using Microsoft.Extensions.DependencyInjection;
using Orders.Api.IntegrationEvents.Consumers;
using Orders.Api.IntegrationEvents.Handlers;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Orders.Api.IntegrationEvents;

public static class IntegrationEventsRegistration
{
    public static IServiceCollection AddOrdersIntegrationEvents(this IServiceCollection services)
    {
        services.AddEventBus();

        services.AddScoped<IIntegrationEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductRenamedEvent>, ProductRenamedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ProductPriceChangedEvent>, ProductPriceChangedEventHandler>();
        services.AddScoped<IIntegrationEventHandler<CategoryUpdatedEvent>, CategoryUpdatedEventHandler>();

        services.AddHostedService<OrdersCatalogEventsConsumer>();

        return services;
    }
}
