using Catalog.DAL.Data;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Catalog.Api.IntegrationEvents.Handlers;

public sealed class ReviewCreatedEventHandler(
    CatalogDbContext db,
    ILogger<ReviewCreatedEventHandler> logger) : IIntegrationEventHandler<ReviewCreatedEvent>
{
    public async Task HandleAsync(ReviewCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // 9. Idempotency check with ProcessedEvents table in same transaction
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed. Skipping.", @event.EventId);
            return;
        }

        var product = await db.Products.FindAsync([@event.ProductId], cancellationToken);
        if (product is not null)
        {
            product.UpdateRating(@event.NewAverageRating, @event.TotalReviews);
            logger.LogInformation("Updated product {ProductId} rating to {Rating} (reviews: {Count})",
                @event.ProductId, @event.NewAverageRating, @event.TotalReviews);
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(ReviewCreatedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
