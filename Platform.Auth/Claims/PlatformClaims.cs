using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Platform.Auth.Claims;

public static class PlatformClaims
{
    public const string Sub = "sub";
    public const string Username = "username";
    public const string Roles = "roles";
    public const string Permission = "permission";
    public const string Scope = "scope";
    public const string ClientId = "client_id";
}

public class KeycloakClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null) return Task.FromResult(principal);

        // Transform realm_access.roles into ClaimTypes.Role / roles
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccess))
        {
            try
            {
                using var doc = JsonDocument.Parse(realmAccess);
                if (doc.RootElement.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName) && !principal.HasClaim(ClaimTypes.Role, roleName))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            identity.AddClaim(new Claim("roles", roleName));
                        }
                    }
                }
            }
            catch
            {
                // ignore json parse error
            }
        }

        // Transform preferred_username to username
        var preferredUsername = principal.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(preferredUsername) && !principal.HasClaim(ClaimTypes.Name, preferredUsername))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, preferredUsername));
            identity.AddClaim(new Claim("username", preferredUsername));
        }

        return Task.FromResult(principal);
    }
}
