using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string FirstName,
    [Required] string LastName);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    string TokenType,
    string UserId,
    string Email,
    IReadOnlyList<string> Roles);

public record RefreshTokenRequest(string? RefreshToken);

public record RevokeTokenRequest(string? RefreshToken);

public record VerifyEmailRequest(
    [Required] string UserId,
    [Required] string Token);

public record ResendVerificationRequest(
    [Required, EmailAddress] string Email);

public record ForgotPasswordRequest(
    [Required, EmailAddress] string Email);

public record ResetPasswordRequest(
    [Required] string UserId,
    [Required] string Token,
    [Required, MinLength(6)] string NewPassword);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword);

public record UserDto(
    string Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);

public record UpdateUserRequest(
    string FirstName,
    string LastName);
