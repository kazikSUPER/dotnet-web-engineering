using Microsoft.Extensions.DependencyInjection;
using Platform.EventBus.Publishing;
using Platform.EventBus.Topology;

namespace Platform.EventBus.Consuming;

public static class EventBusServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<RabbitMqTopologyHostedService>();
        return services;
    }
}
