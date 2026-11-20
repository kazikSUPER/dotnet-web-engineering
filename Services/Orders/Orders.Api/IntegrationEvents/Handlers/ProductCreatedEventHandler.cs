using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.DAL.Data;
using Orders.Domain.Entities;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class ProductCreatedEventHandler(
    OrdersDbContext db,
    ILogger<ProductCreatedEventHandler> logger) : IIntegrationEventHandler<ProductCreatedEvent>
{
    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // 9. Idempotency check with ProcessedEvents table in same transaction
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed by Orders. Skipping.", @event.EventId);
            return;
        }

        logger.LogInformation("Orders replica updated: product {ProductId} '{Name}' created (price: {Price}, stock: {Stock})",
            @event.ProductId, @event.Name, @event.Price, @event.Stock);

        // Update local products table if table exists
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = {@event.ProductId}) INSERT INTO dbo.Products (Id, Name, Price, Stock) VALUES ({@event.ProductId}, {@event.Name}, {@event.Price}, {@event.Stock})",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update local Products table replica for product {ProductId}", @event.ProductId);
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(ProductCreatedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
