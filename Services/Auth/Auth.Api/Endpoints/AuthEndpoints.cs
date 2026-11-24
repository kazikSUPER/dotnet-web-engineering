using Auth.Api.Contracts;
using Auth.Api.Data;
using Auth.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public const string RefreshTokenCookieName = "refreshToken";

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/auth").WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("Auth.Register");
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                return Results.Conflict(new { message = "Користувач з такою поштою вже існує." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // Assign standard role User
            await userManager.AddToRoleAsync(user, "User");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            logger.LogInformation("Email verification token for user {UserId} ({Email}): {Token}", user.Id, user.Email, token);

            return Results.Ok(new
            {
                message = "Реєстрація успішна. Будь ласка, підтвердіть вашу електронну пошту.",
                userId = user.Id,
                emailConfirmationToken = token // Provided for easy automated testing & verification
            });
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwtService,
            HttpContext httpContext) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.BadRequest(new { message = "Невірний email або пароль." });
            }

            // 4. Login blocked for unconfirmed email
            if (!user.EmailConfirmed)
            {
                return Results.BadRequest(new { message = "Email не підтверджено. Будь ласка, підтвердіть пошту перед входом." });
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Results.BadRequest(new { message = "Невірний email або пароль." });
            }

            var accessToken = await jwtService.GenerateAccessTokenAsync(user);
            var refreshToken = await jwtService.GenerateRefreshTokenAsync(user.Id);

            SetRefreshTokenCookie(httpContext, refreshToken.Token, refreshToken.ExpiresAt);

            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new AuthResponse(
                accessToken,
                900, // 15 min
                "Bearer",
                user.Id,
                user.Email!,
                roles.ToList()));
        });

        group.MapPost("/refresh-token", async (
            RefreshTokenRequest? body,
            JwtTokenService jwtService,
            HttpContext httpContext) =>
        {
            var token = httpContext.Request.Cookies[RefreshTokenCookieName] ?? body?.RefreshToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.BadRequest(new { message = "Refresh token не знайдено." });
            }

            try
            {
                var (newAccessToken, newRefreshToken) = await jwtService.RotateRefreshTokenAsync(token);
                SetRefreshTokenCookie(httpContext, newRefreshToken.Token, newRefreshToken.ExpiresAt);

                return Results.Ok(new
                {
                    accessToken = newAccessToken,
                    expiresIn = 900,
                    tokenType = "Bearer"
                });
            }
            catch (Exception)
            {
                ClearRefreshTokenCookie(httpContext);
                return Results.Unauthorized();
            }
        });

        group.MapPost("/revoke-token", async (
            RevokeTokenRequest? body,
            JwtTokenService jwtService,
            HttpContext httpContext) =>
        {
            var token = httpContext.Request.Cookies[RefreshTokenCookieName] ?? body?.RefreshToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                await jwtService.RevokeTokenAsync(token);
            }

            ClearRefreshTokenCookie(httpContext);
            return Results.Ok(new { message = "Токен успішно відкликано." });
        });

        group.MapPost("/logout", async (
            JwtTokenService jwtService,
            HttpContext httpContext) =>
        {
            var token = httpContext.Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrWhiteSpace(token))
            {
                await jwtService.RevokeTokenAsync(token);
            }

            ClearRefreshTokenCookie(httpContext);
            return Results.Ok(new { message = "Успішний вихід із системи." });
        });

        group.MapPost("/verify-email", async (
            VerifyEmailRequest request,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Results.BadRequest(new { message = "Користувача не знайдено." });
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { message = "Недійсний токен підтвердження email.", errors = result.Errors.Select(e => e.Description) });
            }

            return Results.Ok(new { message = "Email успішно підтверджено." });
        });

        group.MapPost("/resend-verification", async (
            ResendVerificationRequest request,
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Ok(new { message = "Якщо такий користувач існує, токен відправлено." });
            }

            if (user.EmailConfirmed)
            {
                return Results.BadRequest(new { message = "Email уже підтверджено." });
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            loggerFactory.CreateLogger("Auth.Resend").LogInformation("New email verification token: {Token}", token);

            return Results.Ok(new
            {
                message = "Токен підтвердження відправлено.",
                emailConfirmationToken = token
            });
        });

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            ILoggerFactory loggerFactory) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Ok(new { message = "Якщо вказаний email існує, інструкції відправлено." });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            loggerFactory.CreateLogger("Auth.ForgotPassword").LogInformation("Password reset token for {UserId}: {Token}", user.Id, token);

            return Results.Ok(new
            {
                message = "Токен відновлення пароля сформовано.",
                userId = user.Id,
                resetToken = token
            });
        });

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwtService) =>
        {
            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                return Results.BadRequest(new { message = "Невірний запит." });
            }

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            // 4. Revoke ALL active refresh tokens on password reset
            await jwtService.RevokeAllUserTokensAsync(user.Id);

            return Results.Ok(new { message = "Пароль успішно скинуто. Усі активні сесії завершено." });
        });

        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.NotFound();
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            return Results.Ok(new { message = "Пароль успішно змінено." });
        }).RequireAuthorization();

        return group;
    }

    private static void SetRefreshTokenCookie(HttpContext context, string token, DateTime expires)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Allow HTTP in development
            SameSite = SameSiteMode.Strict,
            Expires = expires
        };

        context.Response.Cookies.Append(RefreshTokenCookieName, token, cookieOptions);
    }

    private static void ClearRefreshTokenCookie(HttpContext context)
    {
        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        });
    }
}
