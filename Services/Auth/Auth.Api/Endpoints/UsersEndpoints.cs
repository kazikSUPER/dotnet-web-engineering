using Auth.Api.Contracts;
using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Auth.Api.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/users").WithTags("Users").RequireAuthorization();

        // 5. Admin-only: list all users
        group.MapGet("/", async (
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user) =>
        {
            if (!user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var users = await userManager.Users.ToListAsync();
            var dtos = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                dtos.Add(new UserDto(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.Email ?? string.Empty,
                    u.FirstName,
                    u.LastName,
                    u.EmailConfirmed,
                    roles.ToList(),
                    u.CreatedAt));
            }

            return Results.Ok(dtos);
        });

        // 5. Admin or Owner: get user by id
        group.MapGet("/{id}", async (
            string id,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            var isAdmin = user.IsInRole("Admin");

            if (!isAdmin && currentUserId != id)
            {
                return Results.Forbid();
            }

            var targetUser = await userManager.FindByIdAsync(id);
            if (targetUser is null)
            {
                return Results.NotFound();
            }

            var roles = await userManager.GetRolesAsync(targetUser);
            return Results.Ok(new UserDto(
                targetUser.Id,
                targetUser.UserName ?? string.Empty,
                targetUser.Email ?? string.Empty,
                targetUser.FirstName,
                targetUser.LastName,
                targetUser.EmailConfirmed,
                roles.ToList(),
                targetUser.CreatedAt));
        });

        // 5. Admin or Owner: update user
        group.MapPut("/{id}", async (
            string id,
            UpdateUserRequest request,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user) =>
        {
            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            var isAdmin = user.IsInRole("Admin");

            if (!isAdmin && currentUserId != id)
            {
                return Results.Forbid();
            }

            var targetUser = await userManager.FindByIdAsync(id);
            if (targetUser is null)
            {
                return Results.NotFound();
            }

            targetUser.FirstName = request.FirstName;
            targetUser.LastName = request.LastName;
            await userManager.UpdateAsync(targetUser);

            return Results.Ok(new { message = "Дані користувача оновлено." });
        });

        // 5. Admin-only: delete user
        group.MapDelete("/{id}", async (
            string id,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user) =>
        {
            if (!user.IsInRole("Admin"))
            {
                return Results.Forbid();
            }

            var targetUser = await userManager.FindByIdAsync(id);
            if (targetUser is null)
            {
                return Results.NotFound();
            }

            await userManager.DeleteAsync(targetUser);
            return Results.Ok(new { message = "Користувача видалено." });
        });

        return group;
    }
}
