# Розгортання мікросервісів у Kubernetes (Aspir8 та ручний деплой)

## 1. Архітектура розгортання

Розгортання платформи у Kubernetes реалізовано двома узгодженими підходами:

1. **Автоматична оркестрація через Aspir8 (Aspirate)**:
   - Каталог: `Platform.AppHost/aspirate-output/`.
   - Згенеровано повноцінний набір Kustomize-маніфестів для всієї платформи:
     - Мікросервіси: `catalog-api`, `orders-api`, `reviews-api`, `auth-api`, `payments-api`.
     - Агрегатор та шлюз: `aggregator`, `gateway`.
     - Інфраструктура: `sql`, `mongo`, `redis`, `messaging` (RabbitMQ), `keycloak`.
     - `dashboard.yaml` для телеметрії Aspire Dashboard.
   - Секрети вимкнено в генерації (`--disable-secrets`), конфігурації передаються безпечно.

2. **Ручний деплой мікросервісів (`deploy/k8s/`)**:
   - `deployment.yaml`:
     - 2 репліки (`replicas: 2`) для забезпечення High Availability.
     - `imagePullPolicy: IfNotPresent` для локальних образів.
     - `containerPort: 8080`.
     - `livenessProbe`: `/health/live` (перевірка стану процесу).
     - `readinessProbe`: `/health/ready` (перевірка готовності БД, кешу та брокера).
     - Підключення конфігурацій через `envFrom` (`platform-config`, `platform-secrets`).
   - `service.yaml`:
     - Сервіси типу `ClusterIP` з фіксованими DNS-іменами (`catalog-api`, `gateway`).
     - Мапінг портів 80 -> 8080.
   - `configmap.yaml`:
     - Ключі формату `services__<name>__http__0` для коректної роботи .NET Service Discovery усередині K8s кластера.
   - `README.md`:
     - Інструкція зі створення Secret через `kubectl create secret` без збереження паролів у Git.

3. **Багатоетапні Dockerfile (Multi-stage builds)**:
   - `Services/Catalog/Catalog.Api/Dockerfile`:
     - Етап restore: копіюються виключно `.csproj` файли сервісу та залежних бібліотек (`Platform.Auth`, `Platform.EventBus`, `Platform.ServiceDefaults`), після чого виконується `dotnet restore`.
     - Етап build/publish: збірка Release в SDK образі .NET 10.
     - Етап runtime: фінальний легковагий образ на базі `mcr.microsoft.com/dotnet/aspnet:10.0`.
   - `Platform.ApiGateway/Dockerfile`:
     - Аналогічна багатоетапна структура з кешуванням шару відновлення пакетів.
