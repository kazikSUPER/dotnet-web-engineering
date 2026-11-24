# Розгортання мікросервісів у Kubernetes (Manual Deployment)

У цій директорії містяться маніфести Kubernetes для ручного розгортання сервісів платформи:

- `deployment.yaml` — контролери Deployment з 2 репліками (`replicas: 2`), політикою завантаження образів `imagePullPolicy: IfNotPresent`, та налаштованими пробами живучості (`livenessProbe: /health/live`) і готовності (`readinessProbe: /health/ready`).
- `service.yaml` — внутрішні сервіси типу `ClusterIP` з фіксованими DNS-іменами (`catalog-api`, `gateway`) та мапінгом портів 80 -> 8080.
- `configmap.yaml` — конфігураційна мапа `platform-config` з адресами залежностей за схемою `services__<name>__http__0`.
- `secret.yaml` — маніфест Secret (`platform-secrets`) з обліковими даними та рядками підключення.

## Застосування маніфестів

```bash
# 1. Створення конфігурації та секретів
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/secret.yaml

# 2. Запуск сервісів та деплойментів
kubectl apply -f deploy/k8s/service.yaml
kubectl apply -f deploy/k8s/deployment.yaml

# 3. Перевірка статусу
kubectl get pods -w
kubectl get services
```
