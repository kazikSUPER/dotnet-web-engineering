using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Observability;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consuming;

public abstract class RabbitMqConsumerBackgroundService(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    string queueName,
    ILogger logger) : BackgroundService
{
    private const int MaxRetries = 3;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected abstract Type? ResolveEventType(string eventTypeName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting RabbitMQ consumer background service for queue {QueueName}", queueName);

        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var deliveryTag = ea.DeliveryTag;
            var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();

            var eventTypeName = ea.BasicProperties.Type ?? GetHeaderString(headers, "x-event-type") ?? "Unknown";
            var correlationId = GetHeaderString(headers, "x-correlation-id") ?? Guid.NewGuid().ToString();
            var traceParent = GetHeaderString(headers, "traceparent");
            var retryCount = GetHeaderInt(headers, "x-retry-count");

            ActivityContext parentContext = default;
            if (!string.IsNullOrEmpty(traceParent))
            {
                ActivityContext.TryParse(traceParent, null, out parentContext);
            }

            using var activity = MessagingTelemetry.ActivitySource.StartActivity(
                $"Process {eventTypeName}",
                ActivityKind.Consumer,
                parentContext);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", queueName);
            activity?.SetTag("messaging.message.id", ea.BasicProperties.MessageId);
            activity?.SetTag("messaging.operation", "process");
            activity?.SetTag("messaging.retry_count", retryCount);

            var eventType = ResolveEventType(eventTypeName);
            if (eventType is null)
            {
                logger.LogError(
                    "Permanent failure: Unknown event type {EventTypeName} [MessageId: {MessageId}, CorrelationId: {CorrelationId}]. Rejecting to DLQ without requeue.",
                    eventTypeName,
                    ea.BasicProperties.MessageId,
                    correlationId);

                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                MessagingTelemetry.DeadLetteredMessagesCounter.Add(1, new KeyValuePair<string, object?>("queue", queueName));
                return;
            }

            IntegrationEvent? integrationEvent = null;
            try
            {
                integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(ea.Body.Span, eventType, JsonOptions);
                if (integrationEvent is null)
                {
                    throw new PermanentEventException($"Failed to deserialize payload for {eventTypeName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Permanent failure deserializing event {EventTypeName} [MessageId: {MessageId}, CorrelationId: {CorrelationId}]. Sending to DLQ.",
                    eventTypeName,
                    ea.BasicProperties.MessageId,
                    correlationId);

                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                MessagingTelemetry.DeadLetteredMessagesCounter.Add(1, new KeyValuePair<string, object?>("queue", queueName));
                return;
            }

            try
            {
                logger.LogInformation(
                    "Processing event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}, Retry: {RetryCount}] from {Queue}",
                    eventTypeName,
                    integrationEvent.EventId,
                    correlationId,
                    retryCount,
                    queueName);

                using (var scope = scopeFactory.CreateScope())
                {
                    var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    var handler = scope.ServiceProvider.GetService(handlerType);

                    if (handler is null)
                    {
                        throw new PermanentEventException($"No registered handler found for event type {eventTypeName}");
                    }

                    var method = handlerType.GetMethod("HandleAsync");
                    if (method is not null)
                    {
                        var task = (Task?)method.Invoke(handler, [integrationEvent, stoppingToken]);
                        if (task is not null)
                        {
                            await task;
                        }
                    }
                }

                // 8. Ack after success
                await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                MessagingTelemetry.ConsumedMessagesCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventTypeName));

                logger.LogInformation(
                    "Successfully processed event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}]",
                    eventTypeName,
                    integrationEvent.EventId,
                    correlationId);
            }
            catch (PermanentEventException ex)
            {
                logger.LogError(ex,
                    "Permanent failure processing event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}]. Routing to DLQ.",
                    eventTypeName,
                    integrationEvent.EventId,
                    correlationId);

                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                MessagingTelemetry.DeadLetteredMessagesCounter.Add(1, new KeyValuePair<string, object?>("queue", queueName));
            }
            catch (Exception ex)
            {
                // 11. Retry with x-retry-count and maximum attempts
                if (retryCount < MaxRetries)
                {
                    var nextRetry = retryCount + 1;
                    logger.LogWarning(ex,
                        "Transient error handling event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}]. Retrying attempt {NextRetry}/{MaxRetries}",
                        eventTypeName,
                        integrationEvent.EventId,
                        correlationId,
                        nextRetry,
                        MaxRetries);

                    var newHeaders = new Dictionary<string, object?>(headers)
                    {
                        ["x-retry-count"] = nextRetry
                    };

                    var retryProps = new BasicProperties
                    {
                        DeliveryMode = DeliveryModes.Persistent,
                        ContentType = ea.BasicProperties.ContentType ?? "application/json",
                        MessageId = ea.BasicProperties.MessageId,
                        Type = eventTypeName,
                        Headers = newHeaders
                    };

                    // Re-publish to the same queue via default direct exchange ("")
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: retryProps,
                        body: ea.Body,
                        cancellationToken: stoppingToken);

                    // Acknowledge original message to prevent requeue loop
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                else
                {
                    logger.LogError(ex,
                        "Max retries ({MaxRetries}) reached for event {EventType} [EventId: {EventId}, CorrelationId: {CorrelationId}]. Sending to DLQ.",
                        MaxRetries,
                        eventTypeName,
                        integrationEvent.EventId,
                        correlationId);

                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    MessagingTelemetry.DeadLetteredMessagesCounter.Add(1, new KeyValuePair<string, object?>("queue", queueName));
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await channel.CloseAsync(cancellationToken: CancellationToken.None);
    }

    private static string? GetHeaderString(IDictionary<string, object?> headers, string key)
    {
        if (headers.TryGetValue(key, out var val) && val is not null)
        {
            return val is byte[] bytes ? Encoding.UTF8.GetString(bytes) : val.ToString();
        }
        return null;
    }

    private static int GetHeaderInt(IDictionary<string, object?> headers, string key)
    {
        if (headers.TryGetValue(key, out var val) && val is not null)
        {
            if (val is int i) return i;
            if (val is long l) return (int)l;
            if (val is byte[] bytes && int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed)) return parsed;
            if (int.TryParse(val.ToString(), out var strParsed)) return strParsed;
        }
        return 0;
    }
}
