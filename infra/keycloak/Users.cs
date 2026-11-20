using Pulumi.Keycloak;

namespace Infra.Keycloak;

public static class UsersFactory
{
    public static void CreateTestUsers(Realm realm, RealmRoles roles)
    {
        // 1. Admin test user
        var adminUser = new User("user-admin", new UserArgs
        {
            RealmId = realm.Id,
            Username = "admin_user",
            Email = "admin@platform.local",
            FirstName = "Admin",
            LastName = "Master",
            Enabled = true,
            EmailVerified = true,
            InitialPassword = new Pulumi.Keycloak.Inputs.UserInitialPasswordArgs
            {
                Value = "Admin123!",
                Temporary = false
            }
        });

        new UserRoles("user-roles-admin", new UserRolesArgs
        {
            RealmId = realm.Id,
            UserId = adminUser.Id,
            RoleIds = { roles.Admin.Id }
        });

        // 2. Manager test user
        var managerUser = new User("user-manager", new UserArgs
        {
            RealmId = realm.Id,
            Username = "manager_user",
            Email = "manager@platform.local",
            FirstName = "Manager",
            LastName = "Lead",
            Enabled = true,
            EmailVerified = true,
            InitialPassword = new Pulumi.Keycloak.Inputs.UserInitialPasswordArgs
            {
                Value = "Manager123!",
                Temporary = false
            }
        });

        new UserRoles("user-roles-manager", new UserRolesArgs
        {
            RealmId = realm.Id,
            UserId = managerUser.Id,
            RoleIds = { roles.Manager.Id }
        });

        // 3. Regular test user
        var regularUser = new User("user-regular", new UserArgs
        {
            RealmId = realm.Id,
            Username = "regular_user",
            Email = "user@platform.local",
            FirstName = "Regular",
            LastName = "Customer",
            Enabled = true,
            EmailVerified = true,
            InitialPassword = new Pulumi.Keycloak.Inputs.UserInitialPasswordArgs
            {
                Value = "User123!",
                Temporary = false
            }
        });

        new UserRoles("user-roles-regular", new UserRolesArgs
        {
            RealmId = realm.Id,
            UserId = regularUser.Id,
            RoleIds = { roles.User.Id }
        });

        // 4. Guest test user
        var guestUser = new User("user-guest", new UserArgs
        {
            RealmId = realm.Id,
            Username = "guest_user",
            Email = "guest@platform.local",
            FirstName = "Guest",
            LastName = "Visitor",
            Enabled = true,
            EmailVerified = true,
            InitialPassword = new Pulumi.Keycloak.Inputs.UserInitialPasswordArgs
            {
                Value = "Guest123!",
                Temporary = false
            }
        });

        new UserRoles("user-roles-guest", new UserRolesArgs
        {
            RealmId = realm.Id,
            UserId = guestUser.Id,
            RoleIds = { roles.Guest.Id }
        });
    }
}
