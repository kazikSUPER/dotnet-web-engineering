# Документація резильєнтності, метрик та перевірок працездатності (Health Checks)

## 1. Політики резильєнтності (Polly & gRPC ServiceConfig)

### 1.1. Глобальний Standard Resilience Handler (`Platform.ServiceDefaults`)
У `Extensions.ConfigureHttpClientDefaults`:
- **Retry**: Exponential backoff, jitter (`UseJitter = true`), максимум 3 спроби. Вимкнено для небезпечних HTTP-методів (POST, PUT, DELETE, PATCH) через `DisableForUnsafeHttpMethods()`.
- **Circuit Breaker**: `FailureRatio = 0.5`, `MinimumThroughput = 10`, `SamplingDuration = 30s`, `BreakDuration = 15s`. Події зміни стану (`OnOpened`, `OnClosed`, `OnHalfOpened`) логуються структуровано у Serilog.
- **Таймаути**: `AttemptTimeout = 3s`, `TotalRequestTimeout = 10s`.

### 1.2. Запобігання нашаруванню (Layering Prevention)
Для клієнтів із власними політиками (зокрема typed gRPC-клієнтів та спеціалізованих HTTP-клієнтів) викликається `.RemoveAllResilienceHandlers()` перед додаванням індивідуальних обробників або `ServiceConfig`. Це гарантує відсутність подвійного повторення запитів (double retry).

### 1.3. gRPC Retry Policies (`Platform.Aggregator`)
- **Orders.Api & Catalog.Api (Critical)**:
  - `MaxAttempts = 5`
  - `InitialBackoff = 1s`, `MaxBackoff = 5s`, `BackoffMultiplier = 2`
  - `RetryableStatusCodes = [StatusCode.Unavailable, StatusCode.DeadlineExceeded]`
- **Reviews.WebApi (Non-Critical)**:
  - `MaxAttempts = 2`
  - `InitialBackoff = 500ms`, `MaxBackoff = 2s`, `BackoffMultiplier = 2`
  - Швидкий перехід у режим деградації (graceful partial response).

---

## 2. Відмовостійкість кешу Redis (Graceful Degradation)
- `TwoLevelCacheService` у сервісах та `AggregatedCache` в Aggregator перехоплюють винятки при роботі з Redis.
- При відмові Redis фіксується лічильник `cache.redis.failures`, виводиться Warning у логи, і дані прозоро зчитуються з БД.
- Health Check для Redis реєструється з `failureStatus: HealthStatus.Degraded`. Відмова Redis повертає HTTP 200 зі статусом `Degraded` і не блокує readiness probe.

---

## 3. Бізнес-метрики (`IMeterFactory`)
- Класи метрик (`OrdersMetrics`, `CatalogMetrics`, `ReviewsMetrics`, `AggregatorMetrics`) створюються як Singleton і приймають `IMeterFactory`.
- Лічильники:
  - `orders.created`, `payments.processed`
  - `products.viewed`
  - `reviews.created`, `reviews.moderated`
  - `aggregate.upstream.failures`
- Гістограми тривалості (для успішних і невдалих операцій):
  - `orders.duration`
  - `products.duration`
  - `reviews.operation.duration`
  - `aggregate.duration`
- Індикатори стану (`ObservableGauge`):
  - `orders.pending`
  - `products.available`
  - `reviews.moderation.pending`
- Теги: виключно низька кардинальність (`status`, `payment_method`, `operation`, `category`, `action`, `result`, `service`). Жодних чутливих або висококардинальних ідентифікаторів (userId, orderId).

---

## 4. Health Checks
- `/health/live`: перевірка працездатності процесу (`tags: ["live"]`).
- `/health/ready`: перевірка готовності залежностей (`tags: ["ready"]`), повертає деталізований JSON.
- `/health`: загальний стан усіх перевірок.
- Downstream gRPC Health Check: перевіряє стан gRPC-сервісів через стандартний gRPC Health Checking Protocol (`Grpc.Health.V1.Health.HealthClient`).
- AppHost конфігурація: очікування стану готовності через `.WithHttpHealthCheck("/health/ready")`.
