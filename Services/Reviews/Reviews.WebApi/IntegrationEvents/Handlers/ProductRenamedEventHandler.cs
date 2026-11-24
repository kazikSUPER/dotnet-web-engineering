using Bakery.Reviews.Application.Commands;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Reviews.WebApi.IntegrationEvents.Handlers;

public sealed class ProductRenamedEventHandler(
    MongoDbContext db,
    ISender sender,
    ILogger<ProductRenamedEventHandler> logger) : IIntegrationEventHandler<ProductRenamedEvent>
{
    public async Task HandleAsync(ProductRenamedEvent @event, CancellationToken cancellationToken = default)
    {
        var eventIdStr = @event.EventId.ToString();

        // 9. Idempotency check with processed_events collection
        var alreadyProcessed = await db.ProcessedEvents
            .Find(e => e.EventId == eventIdStr)
            .AnyAsync(cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Event {EventId} already processed in Reviews. Skipping.", @event.EventId);
            return;
        }

        logger.LogInformation("Processing ProductRenamedEvent for ProductId {ProductId}: {OldName} -> {NewName}",
            @event.ProductId, @event.OldName, @event.NewName);

        // Direct UpdateMany by ProductId with timestamp protection against outdated messages
        var filter = Builders<Review>.Filter.Eq(x => x.ProductId, @event.ProductId)
            & Builders<Review>.Filter.Lt(x => x.UpdatedAt, @event.OccurredAt);

        var update = Builders<Review>.Update
            .Set(x => x.ProductName, @event.NewName.Trim())
            .Set(x => x.UpdatedAt, @event.OccurredAt);

        var updateResult = await db.Reviews.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        logger.LogInformation("Direct UpdateMany on Reviews modified {Count} documents for ProductId {ProductId}", updateResult.ModifiedCount, @event.ProductId);

        // Also trigger command for domain notifications if needed
        await sender.Send(new RenameProductCommand(@event.ProductId, @event.NewName), cancellationToken);

        // Record processed event
        await db.ProcessedEvents.InsertOneAsync(new ProcessedEventDocument
        {
            EventId = eventIdStr,
            EventType = nameof(ProductRenamedEvent),
            ProcessedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        logger.LogInformation("Successfully updated product name and recorded event {EventId} in Reviews.", @event.EventId);
    }
}
