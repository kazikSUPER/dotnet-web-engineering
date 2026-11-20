using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Platform.EventBus.Observability;

public static class MessagingTelemetry
{
    public const string ActivitySourceName = "Platform.EventBus";
    public const string MeterName = "Platform.EventBus";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> PublishedMessagesCounter = Meter.CreateCounter<long>(
        "messaging.published.count",
        "messages",
        "Number of messages published to the message broker");

    public static readonly Counter<long> ConsumedMessagesCounter = Meter.CreateCounter<long>(
        "messaging.consumed.count",
        "messages",
        "Number of messages consumed from the message broker");

    public static readonly Counter<long> DeadLetteredMessagesCounter = Meter.CreateCounter<long>(
        "messaging.deadlettered.count",
        "messages",
        "Number of messages routed to DLQ");
}
