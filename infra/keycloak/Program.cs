using Pulumi;
using System.Collections.Generic;

namespace Infra.Keycloak;

public class Program
{
    public static Task<int> Main() => Deployment.RunAsync(() =>
    {
        var realm = RealmFactory.CreateRealm();
        var roles = RolesFactory.CreateRoles(realm);

        var clients = ClientsFactory.CreateClientsAndResourceServers(realm);
        UsersFactory.CreateTestUsers(realm, roles);

        return new Dictionary<string, object?>
        {
            ["RealmName"] = realm.RealmName,
            ["RealmUrl"] = Output.Format($"http://localhost:8080/realms/{realm.RealmName}"),
            ["ClientIds"] = Output.Format($"{clients.WebClient.ClientId}, {clients.AggregatorClient.ClientId}"),
            ["AdminRoleId"] = roles.Admin.Id,
            ["ManagerRoleId"] = roles.Manager.Id,
            ["UserRoleId"] = roles.User.Id,
            ["GuestRoleId"] = roles.Guest.Id
        };
    });
}
