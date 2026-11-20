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

            var provider = authOptions.Provider.ToLowerInvariant();

            if (provider == "identity")
            {
                // Self-contained ASP.NET Identity with symmetric key
                var key = Encoding.UTF8.GetBytes(authOptions.SigningKey);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(authOptions.Authority),
                    ValidIssuer = authOptions.Authority,
                    ValidateAudience = !string.IsNullOrEmpty(authOptions.Audience),
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            }
            else if (provider is "duende" or "keycloak")
            {
                // OIDC Authority with JWKS endpoint
                options.Authority = authOptions.Authority;
                options.Audience = authOptions.Audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Authority,
                    ValidateAudience = !string.IsNullOrEmpty(authOptions.Audience),
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
            }

            configureJwt?.Invoke(options);
        });

        services.AddTransient<IClaimsTransformation, KeycloakClaimsTransformation>();

        return services;
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
        });

        return services;
    }
}
