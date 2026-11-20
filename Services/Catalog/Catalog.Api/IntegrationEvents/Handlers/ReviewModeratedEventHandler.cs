using Catalog.DAL.Data;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Catalog.Api.IntegrationEvents.Handlers;

public sealed class ReviewModeratedEventHandler(
    CatalogDbContext db,
    ILogger<ReviewModeratedEventHandler> logger) : IIntegrationEventHandler<ReviewModeratedEvent>
{
    public async Task HandleAsync(ReviewModeratedEvent @event, CancellationToken cancellationToken = default)
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
            logger.LogInformation("Updated product {ProductId} rating after moderation to {Rating} (reviews: {Count})",
                @event.ProductId, @event.NewAverageRating, @event.TotalReviews);
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(ReviewModeratedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
