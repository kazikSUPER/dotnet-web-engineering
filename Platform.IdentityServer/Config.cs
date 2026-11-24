using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
        new IdentityResource("roles", "Користувацькі ролі", ["role", "roles"])
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new("orders.read", "Читання замовлень"),
        new("orders.write", "Створення та редагування замовлень"),
        new("products.read", "Перегляд каталогу товарів"),
        new("products.write", "Управління товарами каталогу"),
        new("categories.read", "Перегляд категорій"),
        new("categories.write", "Управління категоріями"),
        new("reviews.read", "Читання відгуків"),
        new("reviews.write", "Публікація відгуків"),
        new("reviews.moderate", "Модерація відгуків"),
        new("payments.read", "Перегляд оплат"),
        new("payments.write", "Проведення оплат"),
        new("payments.all", "Повний доступ до платіжного шлюзу")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new("orders-api", "Orders Service API")
        {
            Scopes = { "orders.read", "orders.write" }
        },
        new("products-api", "Catalog Service API")
        {
            Scopes = { "products.read", "products.write", "categories.read", "categories.write" }
        },
        new("reviews-api", "Reviews Service API")
        {
            Scopes = { "reviews.read", "reviews.write", "reviews.moderate" }
        },
        new("payments-api", "Payments Service API")
        {
            Scopes = { "payments.read", "payments.write", "payments.all" }
        },
        new("aggregator-api", "Aggregator Service API")
        {
            Scopes = { "orders.read", "products.read", "reviews.read" }
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // 8. Client 1: postman — Authorization Code (+PKCE)
        new()
        {
            ClientId = "postman",
            ClientName = "Postman OAuth Client",
            AllowedGrantTypes = GrantTypes.Code,
            ClientSecrets = { new Secret("postman-secret".Sha256()) },
            RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
            AllowedScopes =
            {
                "openid", "profile", "email", "roles",
                "orders.read", "orders.write",
                "products.read", "products.write",
                "reviews.read", "reviews.write", "reviews.moderate",
                "payments.read", "payments.write"
            },
            AllowOfflineAccess = true,
            RequirePkce = true,
            RequireConsent = false
        },

        // 8. Client 2: aggregator — Client Credentials (M2M)
        new()
        {
            ClientId = "aggregator",
            ClientName = "Aggregator Microservice Client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("aggregator-secret".Sha256()) },
            AllowedScopes =
            {
                "orders.read",
                "orders.write",
                "products.read",
                "products.write",
                "reviews.read",
                "reviews.write"
            }
        },

        // 8. Client 3: swagger — Public Client with PKCE without secret
        new()
        {
            ClientId = "swagger",
            ClientName = "Swagger UI Public Client",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = true,
            RedirectUris =
            {
                "http://localhost:5000/swagger/oauth2-redirect.html",
                "http://localhost:5300/swagger/oauth2-redirect.html"
            },
            AllowedScopes =
            {
                "openid", "profile", "email", "roles",
                "orders.read", "orders.write",
                "products.read", "products.write",
                "reviews.read", "reviews.write",
                "payments.read", "payments.write"
            },
            AllowAccessTokensViaBrowser = true,
            RequireConsent = false
        }
    ];
}
