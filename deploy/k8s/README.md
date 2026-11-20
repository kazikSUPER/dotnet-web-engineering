# Розгортання мікросервісів у Kubernetes (Manual Deployment)

У цій директорії містяться маніфести Kubernetes для ручного розгортання сервісів платформи:

- `deployment.yaml` — контролери Deployment з 2 репліками (`replicas: 2`), політикою завантаження образів `imagePullPolicy: IfNotPresent`, та налаштованими пробами живучості (`livenessProbe: /health/live`) і готовності (`readinessProbe: /health/ready`).
- `service.yaml` — внутрішні сервіси типу `ClusterIP` з фіксованими DNS-іменами (`catalog-api`, `gateway`) та мапінгом портів 80 -> 8080.
- `configmap.yaml` — конфігураційна мапа `platform-config` з адресами залежностей за схемою `services__<name>__http__0`.

## Безпечне створення Secret (поза репозиторієм)

Згідно з вимогами безпеки, секрети (паролі БД, JWT-ключі) **НЕ коммітяться у Git-репозиторій**.
Перед застосуванням маніфестів створіть секрет у кластері вручну:

```bash
kubectl create secret generic platform-secrets \
  --from-literal=ConnectionStrings__CatalogDb="Server=dotnet-web-sql;Database=CatalogDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True" \
  --from-literal=ConnectionStrings__OrdersDb="Server=dotnet-web-sql;Database=OrdersDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True" \
  --from-literal=ConnectionStrings__IdentityDb="Server=dotnet-web-sql;Database=IdentityDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True" \
  --from-literal=ConnectionStrings__redis="dotnet-web-redis:6379" \
  --from-literal=ConnectionStrings__messaging="amqp://guest:guest@dotnet-web-rabbitmq:5672" \
  --from-literal=Auth__SigningKey="SuperSecretAuthSigningKeyWithAtLeast32BytesForHmacSha256Security!"
```

## Застосування маніфестів

```bash
# 1. Створення конфігурації
kubectl apply -f deploy/k8s/configmap.yaml

# 2. Запуск сервісів та деплойментів
kubectl apply -f deploy/k8s/service.yaml
kubectl apply -f deploy/k8s/deployment.yaml

# 3. Перевірка статусу
kubectl get pods -w
kubectl get services
```
