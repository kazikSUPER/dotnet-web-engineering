# Аутентифікація та авторизація (Identity, IdentityServer, Keycloak + Pulumi)

## 1. Архітектура безпеки

У платформі реалізовано комплексну систему контролю доступу на базі трьох провайдерів ідентифікації з єдиною моделлю дозволів `[RequirePermission]`:

1. **ASP.NET Identity + JWT (`Services/Auth/Auth.Api`)**:
   - Власна база `IdentityDb` на SQL Server через .NET Aspire.
   - `ApplicationUser : IdentityUser` з полями `FirstName`, `LastName`.
   - Access-токени (15 хв) з claims: `sub`, `username`, `roles`, `email`, `jti`.
   - Refresh-токени (7 днів) у захищених `HttpOnly` cookies з ідемпотентною ротацією в БД.
   - Ендпоінти: `/auth/register`, `/auth/login` (суворе блокування непідтвердженого email `RequireConfirmedEmail = true`), `/auth/refresh-token`, `/auth/revoke-token`, `/auth/logout`, `/auth/verify-email`, `/auth/forgot-password`, `/auth/reset-password` (відкликає всі активні сесії користувача), `/auth/change-password`.
   - Захищені ендпоінти `/users` з перевіркою ролей (`Admin` або власник акаунта).

2. **Duende IdentityServer (`Platform.IdentityServer`)**:
   - Окрема база `IdentityServerDb`.
   - Підтримка трьох клієнтів:
     - `postman` — Authorization Code Flow (+PKCE)
     - `aggregator` — Client Credentials Flow (M2M)
     - `swagger` — Public Client з PKCE без секрету
   - ApiResources та ApiScopes (`orders.read`, `orders.write`, `products.read`, `products.write`, `categories.read`, `categories.write`, `reviews.read`, `reviews.write`, `reviews.moderate`, `payments.read`, `payments.write`, `payments.all`).
   - Quickstart UI.

3. **Keycloak + Pulumi IaC (`infra/keycloak/`)**:
   - Декларативний опис інфраструктури Keycloak через C# Pulumi:
     - Realm: `BakeryRealm`
     - Resource Servers: `orders-api`, `products-api`, `payments-api`
     - Клієнти: `platform-web` (Code), `platform-aggregator` (Client Credentials)
     - Ролі: `admin`, `manager`, `user`, `guest` згідно з матрицею прав
     - Тестові користувачі: `admin_user`, `manager_user`, `regular_user`, `guest_user`
     - Protocol mapper для прокидання аудиторії та дозволів у JWT токен.

## 2. Матриця прав та Permission-based авторизація (`Platform.Auth`)

Кожен бізнес-ендпоінт мікросервісів захищено атрибутом `[RequirePermission("<permission>")]`:

| Дозвіл (Permission) | admin | manager | user | guest |
|---|:---:|:---:|:---:|:---:|
| `orders.read` | ✓ | ✓ | ✓ (власні) | — |
| `orders.write` | ✓ | ✓ | ✓ (власні) | — |
| `orders.cancel` | ✓ | ✓ | — | — |
| `customers.read` | ✓ | ✓ | ✓ (власні) | — |
| `customers.write` | ✓ | ✓ | ✓ (власні) | — |
| `products.read` | ✓ | ✓ | ✓ | ✓ |
| `products.write` | ✓ | ✓ | — | — |
| `categories.read` | ✓ | ✓ | ✓ | ✓ |
| `categories.write` | ✓ | ✓ | — | — |
| `reviews.read` | ✓ | ✓ | ✓ | ✓ |
| `reviews.write` | ✓ | ✓ | ✓ (власні) | — |
| `reviews.moderate` | ✓ | ✓ | — | — |
| `payments.read` | ✓ | — | — | — |
| `payments.write` | ✓ | — | — | — |

Платіжний сервіс (`Services/Payments/Payments.Api`) доступний **виключно для ролі admin**.
