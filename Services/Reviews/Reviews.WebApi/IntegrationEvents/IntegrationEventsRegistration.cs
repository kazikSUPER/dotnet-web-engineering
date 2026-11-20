using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;
using Reviews.WebApi.IntegrationEvents.Consumers;
using Reviews.WebApi.IntegrationEvents.Handlers;

namespace Reviews.WebApi.IntegrationEvents;

public static class IntegrationEventsRegistration
{
    public static IServiceCollection AddReviewsIntegrationEvents(this IServiceCollection services)
    {
        services.AddEventBus();
        services.AddScoped<IIntegrationEventHandler<ProductRenamedEvent>, ProductRenamedEventHandler>();
        services.AddHostedService<ReviewsCatalogEventsConsumer>();

        return services;
    }
}
