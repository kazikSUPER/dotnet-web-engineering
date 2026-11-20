using Auth.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Platform.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Services;

public class JwtTokenService(
    IOptions<AuthOptions> authOptions,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<JwtTokenService> logger)
{
    private readonly AuthOptions _options = authOptions.Value;

    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new("username", user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // 15 min access token
            Issuer = _options.Authority,
            Audience = _options.Audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string? replacedByToken = null)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var tokenString = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days refresh token
            ReplacedByToken = replacedByToken
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<(string AccessToken, RefreshToken NewRefreshToken)> RotateRefreshTokenAsync(string token)
    {
        var existingToken = await dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);

        if (existingToken is null || !existingToken.IsActive || existingToken.User is null)
        {
            logger.LogWarning("Invalid or expired refresh token attempt: {Token}", token);
            throw new SecurityTokenException("Недійсний або прострочений refresh token.");
        }

        // Revoke the old token
        existingToken.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var newAccessToken = await GenerateAccessTokenAsync(existingToken.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(existingToken.UserId, existingToken.Token);

        existingToken.ReplacedByToken = newRefreshToken.Token;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Refreshed tokens for user {UserId}", existingToken.UserId);
        return (newAccessToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string token)
    {
        var existingToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
        if (existingToken is not null && existingToken.IsActive)
        {
            existingToken.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Revoked refresh token for user {UserId}", existingToken.UserId);
        }
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var t in activeTokens)
        {
            t.RevokedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Revoked all ({Count}) active refresh tokens for user {UserId}", activeTokens.Count, userId);
    }
}
