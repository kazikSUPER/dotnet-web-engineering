using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus.Topology;

public sealed class RabbitMqTopologyHostedService(
    IConnection connection,
    ILogger<RabbitMqTopologyHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Declaring RabbitMQ topology (topic exchange, queues, bindings, DLX/DLQ)...");

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // 1. Declare Topic Exchange & DLX
        await channel.ExchangeDeclareAsync(
            exchange: EventBusTopology.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: EventBusTopology.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        // 2. Setup Queues with DLQ
        var queues = new[]
        {
            (Queue: EventBusTopology.OrdersCatalogEventsQueue, RoutingKeys: new[] { "catalog.product.*", "catalog.category.*" }),
            (Queue: EventBusTopology.BakeryCatalogEventsQueue, RoutingKeys: new[] { "catalog.product.*", "catalog.category.*" }),
            (Queue: EventBusTopology.ReviewsCatalogEventsQueue, RoutingKeys: new[] { "catalog.product.*" }),
            (Queue: EventBusTopology.CatalogReviewEventsQueue, RoutingKeys: new[] { "reviews.review.*" })
        };

        foreach (var (queueName, routingKeys) in queues)
        {
            var dlqName = EventBusTopology.GetDeadLetterQueueName(queueName);

            // Declare DLQ
            await channel.QueueDeclareAsync(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Bind DLQ to DLX
            await channel.QueueBindAsync(
                queue: dlqName,
                exchange: EventBusTopology.DeadLetterExchangeName,
                routingKey: dlqName,
                cancellationToken: cancellationToken);

            // Declare Main Queue with x-dead-letter-exchange
            var queueArgs = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = EventBusTopology.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = dlqName
            };

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            // Bindings to Topic Exchange
            foreach (var routingKey in routingKeys)
            {
                await channel.QueueBindAsync(
                    queue: queueName,
                    exchange: EventBusTopology.ExchangeName,
                    routingKey: routingKey,
                    cancellationToken: cancellationToken);
            }
        }

        logger.LogInformation("RabbitMQ topology declared successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
