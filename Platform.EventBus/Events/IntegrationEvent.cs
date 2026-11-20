using System.Text.Json.Serialization;

namespace Platform.EventBus.Events;

public abstract record IntegrationEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventRoutingKeyAttribute(string routingKey) : Attribute
{
    public string RoutingKey { get; } = routingKey;
}
