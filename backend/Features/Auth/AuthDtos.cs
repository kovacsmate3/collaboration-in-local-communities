using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Backend.Features.Auth;

[PublicAPI]
public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(120)] string DisplayName,
    [MaxLength(200)] string? Workplace,
    [MaxLength(200)] string? Position,
    [MaxLength(300)] string? LocationText,
    double? Latitude,
    double? Longitude,
    [MaxLength(1000)] string? Bio,
    bool AcceptTerms,
    IReadOnlyList<Guid>? SkillIds);

[PublicAPI]
public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

[PublicAPI]
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string TokenType,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
