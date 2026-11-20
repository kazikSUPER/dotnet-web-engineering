using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

namespace Infra.Keycloak;

public static class ClientsFactory
{
    public static void CreateClientsAndResourceServers(Realm realm)
    {
        // 11. Resource servers
        var ordersApi = new Client("client-orders-api", new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = "orders-api",
            Name = "Orders Service API",
            Enabled = true,
            AccessType = "BEARER-ONLY"
        });

        var productsApi = new Client("client-products-api", new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = "products-api",
            Name = "Catalog Service API",
            Enabled = true,
            AccessType = "BEARER-ONLY"
        });

        var paymentsApi = new Client("client-payments-api", new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = "payments-api",
            Name = "Payments Service API",
            Enabled = true,
            AccessType = "BEARER-ONLY"
        });

        // 12. Client 1: Authorization Code Flow (interactive client)
        var webClient = new Client("client-platform-web", new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = "platform-web",
            Name = "Platform Web Application",
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            ClientSecret = "platform-web-secret-keycloak",
            StandardFlowEnabled = true,
            DirectAccessGrantsEnabled = true,
            ValidRedirectUris =
            {
                "http://localhost:5000/*",
                "https://oauth.pstmn.io/v1/callback"
            }
        });

        // 12. Client 2: Client Credentials (M2M Aggregator)
        var aggregatorClient = new Client("client-platform-aggregator", new ClientArgs
        {
            RealmId = realm.Id,
            ClientId = "platform-aggregator",
            Name = "Platform Aggregator M2M Client",
            Enabled = true,
            AccessType = "CONFIDENTIAL",
            ClientSecret = "aggregator-secret-keycloak",
            ServiceAccountsEnabled = true,
            StandardFlowEnabled = false
        });

        // 12. Protocol mapper: map client realm roles and permissions to token
        var roleMapper = new AudienceProtocolMapper("mapper-audience", new AudienceProtocolMapperArgs
        {
            RealmId = realm.Id,
            ClientId = webClient.Id,
            Name = "audience-mapper",
            IncludedCustomAudience = "platform-api"
        });
    }
}
