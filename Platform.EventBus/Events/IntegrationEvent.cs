using System.Text.Json.Serialization;

namespace Platform.EventBus.Events;

public abstract record IntegrationEvent(
    Guid EventId = default,
    string? CorrelationId = null,
    DateTime OccurredAt = default,
    int Version = 1)
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; } = EventId == default ? Guid.NewGuid() : EventId;

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; init; } = OccurredAt == default ? DateTime.UtcNow : OccurredAt;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; } = CorrelationId;

    [JsonPropertyName("version")]
    public int Version { get; init; } = Version;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventRoutingKeyAttribute(string routingKey) : Attribute
{
    public string RoutingKey { get; } = routingKey;
}
