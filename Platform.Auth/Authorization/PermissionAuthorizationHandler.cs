using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Platform.Auth.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // 1. Admin role grants full superuser access
        if (context.User.IsInRole("Admin") || context.User.IsInRole("admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Direct permission claims
        var hasDirectPermission = context.User.Claims
            .Where(c => c.Type is "permission" or "permissions" or "authorization.permissions")
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasDirectPermission)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 3. Scope claims (Duende / OIDC)
        var hasScope = context.User.Claims
            .Where(c => c.Type is "scope" or "scp")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(s => string.Equals(s, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasScope)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 4. Role-based fallback permission mapping
        if (IsPermittedByRole(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static bool IsPermittedByRole(ClaimsPrincipal user, string permission)
    {
        var isManager = user.IsInRole("Manager") || user.IsInRole("manager");
        var isUser = user.IsInRole("User") || user.IsInRole("user");
        var isGuest = user.IsInRole("Guest") || user.IsInRole("guest");

        if (isManager)
        {
            // Manager has access to everything except payments.*
            return !permission.StartsWith("payments.", StringComparison.OrdinalIgnoreCase);
        }

        if (isUser)
        {
            return permission switch
            {
                "orders.read" or "orders.write" => true,
                "customers.read" or "customers.write" => true,
                "products.read" or "categories.read" => true,
                "reviews.read" or "reviews.write" => true,
                _ => false
            };
        }

        if (isGuest)
        {
            return permission is "products.read" or "categories.read" or "reviews.read";
        }

        return false;
    }
}
