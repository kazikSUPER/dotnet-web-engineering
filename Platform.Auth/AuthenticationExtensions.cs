using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Platform.Auth.Authorization;
using Platform.Auth.Claims;
using System.Security.Claims;
using System.Text;

namespace Platform.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwt = null)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        authBuilder.AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
            options.SaveToken = true;

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey));

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    "http://localhost:5400",
                    "http://localhost:5300",
                    "http://localhost:8080/realms/platform",
                    "http://identityserver:5400",
                    "http://auth-api:5300",
                    authOptions.Authority
                }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray(),

                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.Name,

                IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                {
                    var keys = new List<SecurityKey> { symmetricKey };
                    var isKey = LoadIdentityServerKey();
                    if (isKey is not null)
                    {
                        keys.Add(isKey);
                    }
                    return keys;
                }
            };

            configureJwt?.Invoke(options);
        });

        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        return services;
    }

    private static SecurityKey? _cachedIdentityServerKey;
    private static SecurityKey? LoadIdentityServerKey()
    {
        if (_cachedIdentityServerKey is not null) return _cachedIdentityServerKey;

        try
        {
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "tempkey.jwk"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Platform.IdentityServer", "tempkey.jwk"),
                @"D:\ІФТКН\4 КУРС\1 семестр\Технології .NET у веб-інженерії\+\Platform.IdentityServer\tempkey.jwk"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    _cachedIdentityServerKey = new JsonWebKey(json);
                    return _cachedIdentityServerKey;
                }
            }
        }
        catch
        {
            // Ignore if key is not found yet
        }

        return null;
    }

    public static IServiceCollection AddPlatformAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // Core standard role policies
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "admin"));
            options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager", "manager", "Admin", "admin"));
            options.AddPolicy("UserOnly", policy => policy.RequireRole("User", "user", "Manager", "manager", "Admin", "admin"));

            // Scope policies for M2M (IdentityServer / Keycloak)
            options.AddPolicy("Scope.Orders.Read", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Contains("orders.read"))));
            options.AddPolicy("Scope.Products.Read", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Contains("products.read"))));
            options.AddPolicy("Scope.Payments.All", policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == "scope" && c.Value.Contains("payments.all"))));

            options.AddPolicy("catalog.read", policy => policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("User") || ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Manager") ||
                ctx.User.HasClaim(c => (c.Type == "scope" && (c.Value.Contains("catalog.read") || c.Value.Contains("products.read"))) || (c.Type == "permission" && c.Value == "products.read"))));
            options.AddPolicy("orders.read", policy => policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("User") || ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Manager") ||
                ctx.User.HasClaim(c => (c.Type == "scope" && c.Value.Contains("orders.read")) || (c.Type == "permission" && c.Value == "orders.read"))));
            options.AddPolicy("reviews.read", policy => policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("User") || ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Manager") ||
                ctx.User.HasClaim(c => (c.Type == "scope" && c.Value.Contains("reviews.read")) || (c.Type == "permission" && c.Value == "reviews.read"))));
        });

        return services;
    }
}
