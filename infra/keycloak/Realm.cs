using Pulumi;
using Pulumi.Keycloak;

namespace Infra.Keycloak;

public static class RealmFactory
{
    public static Realm CreateRealm()
    {
        return new Realm("bakery-realm", new RealmArgs
        {
            RealmName = "BakeryRealm",
            Enabled = true,
            DisplayName = "Bakery Platform Realm",
            RegistrationAllowed = true,
            ResetPasswordAllowed = true,
            RememberMe = true,
            VerifyEmail = true,
            LoginWithEmailAllowed = true
        });
    }
}
