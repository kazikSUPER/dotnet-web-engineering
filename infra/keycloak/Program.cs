using Pulumi;
using System.Collections.Generic;

namespace Infra.Keycloak;

public class Program
{
    public static Task<int> Main() => Deployment.RunAsync(() =>
    {
        var realm = RealmFactory.CreateRealm();
        var roles = RolesFactory.CreateRoles(realm);

        ClientsFactory.CreateClientsAndResourceServers(realm);
        UsersFactory.CreateTestUsers(realm, roles);

        return new Dictionary<string, object?>
        {
            ["RealmName"] = realm.RealmName,
            ["AdminRoleId"] = roles.Admin.Id,
            ["ManagerRoleId"] = roles.Manager.Id,
            ["UserRoleId"] = roles.User.Id,
            ["GuestRoleId"] = roles.Guest.Id
        };
    });
}
