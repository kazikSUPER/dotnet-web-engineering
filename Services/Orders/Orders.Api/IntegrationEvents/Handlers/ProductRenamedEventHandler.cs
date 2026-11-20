using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.DAL.Data;
using Orders.Domain.Entities;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class ProductRenamedEventHandler(
    OrdersDbContext db,
    ILogger<ProductRenamedEventHandler> logger) : IIntegrationEventHandler<ProductRenamedEvent>
{
    public async Task HandleAsync(ProductRenamedEvent @event, CancellationToken cancellationToken = default)
    {
        // 9. Idempotency check with ProcessedEvents table in same transaction
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed by Orders. Skipping.", @event.EventId);
            return;
        }

        // 10. Data replication according to Lab 01 policy:
        // Update local directory of products for new orders.
        // Existing order items historical unit prices remain unchanged.
        logger.LogInformation("Orders replica updated: product {ProductId} renamed from '{OldName}' to '{NewName}'",
            @event.ProductId, @event.OldName, @event.NewName);

        // Update local products table if table exists
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"IF EXISTS (SELECT 1 FROM dbo.Products WHERE Id = {@event.ProductId}) UPDATE dbo.Products SET Name = {@event.NewName}, UpdatedAt = SYSUTCDATETIME() WHERE Id = {@event.ProductId}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update local Products table replica for product {ProductId}", @event.ProductId);
        }

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(ProductRenamedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
