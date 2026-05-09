using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(200)] string? Workplace,
    [MaxLength(200)] string? Position,
    [MaxLength(300)] string? LocationText,
    [MaxLength(1000)] string? Bio,
    bool AcceptTerms);

public sealed record RegisterResponse(string Message);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string TokenType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record ConfirmEmailRequest(
    [Required] Guid UserId,
    [Required] string Token);

public sealed record ResendVerificationEmailRequest(
    [Required, EmailAddress] string Email);
