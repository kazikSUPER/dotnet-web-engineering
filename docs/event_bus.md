# Подієва мікросервісна архітектура (RabbitMQ Integration Event Bus)

## 1. Топологія брокера (RabbitMQ Topology)

У проєкті реалізовано подієву модель комунікації на базі брокера RabbitMQ через `Aspire.RabbitMQ.Client` (RabbitMQ.Client 7.x):

- **Exchange**: `platform.events` (типу `topic`, durable)
- **Dead-Letter Exchange (DLX)**: `platform.events.dlx` (типу `topic`, durable)
- **Черги споживачів (Consumer Queues)**:
  - `q.orders.catalog-events` — слухає події каталогу: `catalog.product.*`, `catalog.category.*`
  - `q.reviews.catalog-events` — слухає перейменування товарів: `catalog.product.renamed`
  - `q.catalog.review-events` — слухає події відгуків: `reviews.review.*`
- **Dead-Letter Queues (DLQ)**:
  - `q.orders.catalog-events.dlq`
  - `q.reviews.catalog-events.dlq`
  - `q.catalog.review-events.dlq`

## 2. Контракти подій (Event Contracts)

Всі події успадковують `IntegrationEvent` (`Platform.EventBus/Events/IntegrationEvent.cs`):
- `EventId` (Guid)
- `OccurredAt` (DateTimeOffset)
- `CorrelationId` (string)
- `Version` (int)

Маршрутизація визначається атрибутом `[EventRoutingKey("domain.entity.action")]`:
- `ProductCreatedEvent` (`catalog.product.created`)
- `ProductRenamedEvent` (`catalog.product.renamed`)
- `ProductPriceChangedEvent` (`catalog.product.price-changed`)
- `CategoryUpdatedEvent` (`catalog.category.updated`)
- `OrderPlacedEvent` (`orders.order.placed`)
- `OrderStatusChangedEvent` (`orders.order.status-changed`)
- `PaymentProcessedEvent` (`orders.payment.processed`)
- `ReviewCreatedEvent` (`reviews.review.created`)
- `ReviewModeratedEvent` (`reviews.review.moderated`)

## 3. Гарантії надійності та ідемпотентність

1. **Transactional Publishing**: події публікуються виключно після успішного підтвердження змін у базі даних (`SaveChangesAsync` або коміт транзакції MongoDB).
2. **Idempotency**: кожен сервіс має таблицю/колекцію `ProcessedEvents` (`processed_events` у MongoDB). Перед обробкою виконується перевірка `EventId` у тій же транзакції, що й бізнес-зміни.
3. **Manual Acknowledgment & Error Handling**:
   - Успішна обробка $\to$ `BasicAckAsync`.
   - Несумісні/биті повідомлення або помилки після вичерпання спроб $\to$ `BasicNackAsync(requeue: false)` з перенаправленням у відповідну DLQ.
   - Тимчасові помилки $\to$ повтор через заголовок `x-retry-count` (до 3 спроб).
4. **W3C Distributed Tracing**: заголовок `traceparent` автоматично передається видавцем та витягується споживачем, створюючи `ActivityKind.Consumer` span у системі трасування OpenTelemetry.
