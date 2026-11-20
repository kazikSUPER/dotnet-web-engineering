using Microsoft.AspNetCore.Authorization;

namespace Platform.Auth.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
