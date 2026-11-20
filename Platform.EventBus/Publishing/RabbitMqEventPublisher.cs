using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Observability;
using Platform.EventBus.Topology;
using RabbitMQ.Client;

namespace Platform.EventBus.Publishing;

public sealed class RabbitMqEventPublisher(
    IConnection connection,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var eventType = @event.GetType();
        var routingAttr = eventType.GetCustomAttribute<EventRoutingKeyAttribute>();
        var routingKey = routingAttr?.RoutingKey ?? eventType.Name.ToLowerInvariant();

        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            $"Publish {@event.GetType().Name}",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", EventBusTopology.ExchangeName);
        activity?.SetTag("messaging.destination.routing_key", routingKey);
        activity?.SetTag("messaging.message.id", @event.EventId.ToString());

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonOptions);

        var correlationId = @event.CorrelationId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var traceParent = Activity.Current?.Id;

        var headers = new Dictionary<string, object?>
        {
            ["x-event-type"] = eventType.Name,
            ["x-correlation-id"] = correlationId,
            ["x-retry-count"] = 0
        };

        if (!string.IsNullOrEmpty(traceParent))
        {
            headers["traceparent"] = traceParent;
        }

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = eventType.Name,
            Headers = headers
        };

        logger.LogInformation(
            "Publishing integration event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}] to {Exchange} with key {RoutingKey}",
            eventType.Name,
            @event.EventId,
            correlationId,
            EventBusTopology.ExchangeName,
            routingKey);

        await channel.BasicPublishAsync(
            exchange: EventBusTopology.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: bodyBytes,
            cancellationToken: cancellationToken);

        MessagingTelemetry.PublishedMessagesCounter.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType.Name),
            new KeyValuePair<string, object?>("routing_key", routingKey));
    }
}
