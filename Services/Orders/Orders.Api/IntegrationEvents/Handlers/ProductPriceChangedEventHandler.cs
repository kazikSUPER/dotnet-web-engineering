using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.DAL.Data;
using Orders.Domain.Entities;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class ProductPriceChangedEventHandler(
    OrdersDbContext db,
    ILogger<ProductPriceChangedEventHandler> logger) : IIntegrationEventHandler<ProductPriceChangedEvent>
{
    public async Task HandleAsync(ProductPriceChangedEvent @event, CancellationToken cancellationToken = default)
    {
        // 9. Idempotency check with ProcessedEvents table in same transaction
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed by Orders. Skipping.", @event.EventId);
            return;
        }

        // 10. Data replication according to Lab 01 policy:
        // Update product price for new orders only; historical OrderItems.UnitPrice is NEVER overwritten.
        logger.LogInformation("Orders replica updated: product {ProductId} price changed from {OldPrice} to {NewPrice}",
            @event.ProductId, @event.OldPrice, @event.NewPrice);

        // Update local products table replica with timestamp protection against outdated messages
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = {@event.ProductId}) UPDATE dbo.Products SET Price = {@event.NewPrice}, UpdatedAt = {@event.OccurredAt} WHERE Id = {@event.ProductId} AND (UpdatedAt < {@event.OccurredAt} OR UpdatedAt IS NULL)",
            cancellationToken);

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(ProductPriceChangedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
