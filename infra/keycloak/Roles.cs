using Pulumi.Keycloak;

namespace Infra.Keycloak;

public record RealmRoles(Role Admin, Role Manager, Role User, Role Guest);

public static class RolesFactory
{
    public static RealmRoles CreateRoles(Realm realm)
    {
        var guest = new Role("role-guest", new RoleArgs
        {
            RealmId = realm.Id,
            Name = "guest",
            Description = "Guest with read-only access to public catalog and reviews"
        });

        var user = new Role("role-user", new RoleArgs
        {
            RealmId = realm.Id,
            Name = "user",
            Description = "Standard authenticated user with access to view catalog, own orders and reviews",
            CompositeRoles = { guest.Id }
        });

        var manager = new Role("role-manager", new RoleArgs
        {
            RealmId = realm.Id,
            Name = "manager",
            Description = "Manager with access to catalog, orders and reviews moderation, but no payments access",
            CompositeRoles = { user.Id }
        });

        var admin = new Role("role-admin", new RoleArgs
        {
            RealmId = realm.Id,
            Name = "admin",
            Description = "Administrator with full access to all microservices including payments",
            CompositeRoles = { manager.Id }
        });

        return new RealmRoles(admin, manager, user, guest);
    }
}
