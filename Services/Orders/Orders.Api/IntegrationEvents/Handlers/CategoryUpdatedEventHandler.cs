using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.DAL.Data;
using Orders.Domain.Entities;
using Platform.EventBus.Consuming;
using Platform.EventBus.Events.Contracts;

namespace Orders.Api.IntegrationEvents.Handlers;

public sealed class CategoryUpdatedEventHandler(
    OrdersDbContext db,
    ILogger<CategoryUpdatedEventHandler> logger) : IIntegrationEventHandler<CategoryUpdatedEvent>
{
    public async Task HandleAsync(CategoryUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        // 9. Idempotency check with ProcessedEvents table in same transaction
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == @event.EventId, cancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed by Orders. Skipping.", @event.EventId);
            return;
        }

        logger.LogInformation("Orders replica updated: category {CategoryId} name updated to '{Name}'",
            @event.CategoryId, @event.Name);

        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = @event.EventId,
            EventType = nameof(CategoryUpdatedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
