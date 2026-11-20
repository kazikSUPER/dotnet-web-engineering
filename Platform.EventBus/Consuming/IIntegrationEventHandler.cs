using Platform.EventBus.Events;

namespace Platform.EventBus.Consuming;

public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public class PermanentEventException(string message, Exception? inner = null) : Exception(message, inner);
