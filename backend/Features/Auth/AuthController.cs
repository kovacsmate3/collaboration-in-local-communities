using System.Security.Claims;
using System.Text.Json;
using Backend.Common;
using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IAuthTokenService tokenService) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.AcceptTerms)
        {
            ModelState.AddModelError(nameof(request.AcceptTerms), "Terms must be accepted.");
            return ValidationProblem(ModelState);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim()
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return IdentityValidationProblem(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, ApplicationRoleNames.User);
        if (!roleResult.Succeeded)
        {
            return IdentityValidationProblem(roleResult);
        }

        var now = DateTimeOffset.UtcNow;
        var profile = new UserProfile
        {
            UserId = user.Id,
            DisplayName = request.DisplayName.Trim(),
            Workplace = StringUtilities.Normalize(request.Workplace),
            Position = StringUtilities.Normalize(request.Position),
            LocationText = StringUtilities.Normalize(request.LocationText),
            Bio = StringUtilities.Normalize(request.Bio),
            IsProfileCompleted = true,
            CreatedAt = now,
            UpdatedAt = now,
            PrivacySettings = new ProfilePrivacySettings
            {
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        db.Profiles.Add(profile);

        var activeTerms = await db.TermsVersions
            .Where(terms => terms.IsActive)
            .OrderByDescending(terms => terms.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeTerms is not null)
        {
            db.UserTermsAcceptances.Add(new UserTermsAcceptance
            {
                UserId = user.Id,
                TermsVersionId = activeTerms.Id,
                AcceptedAt = now,
                IpAddress = GetClientIp()
            });
        }

        var tokens = await tokenService.CreateTokenPairAsync(user, cancellationToken);
        db.RefreshTokens.Add(CreateRefreshToken(user.Id, tokens, null));
        AddAuditEvent(user.Id, "auth.registered", "ApplicationUser", user.Id, new { user.Email });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        SetRefreshTokenCookie(tokens);
        return Ok(ToResponse(user, tokens));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            AddAuditEvent(null, "auth.login_failed", null, null, new { request.Email });
            await db.SaveChangesAsync(cancellationToken);
            return Unauthorized();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            AddAuditEvent(user.Id, "auth.login_forbidden", "ApplicationUser", user.Id, new { user.Email });
            await db.SaveChangesAsync(cancellationToken);
            return Forbid();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            AddAuditEvent(user.Id, "auth.login_failed", "ApplicationUser", user.Id, new { user.Email });
            await db.SaveChangesAsync(cancellationToken);
            return Unauthorized();
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var tokens = await tokenService.CreateTokenPairAsync(user, cancellationToken);
        db.RefreshTokens.Add(CreateRefreshToken(user.Id, tokens, null));
        AddAuditEvent(user.Id, "auth.login_succeeded", "ApplicationUser", user.Id, new { user.Email });
        await db.SaveChangesAsync(cancellationToken);

        SetRefreshTokenCookie(tokens);
        return Ok(ToResponse(user, tokens));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var now = DateTimeOffset.UtcNow;
        var clientIp = GetClientIp();

        var activeTokens = await db.RefreshTokens
            .Where(token => token.UserId == userId.Value && token.RevokedAt == null && token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = clientIp;
        }

        AddAuditEvent(userId.Value, "auth.logout", "ApplicationUser", userId.Value, null);
        await db.SaveChangesAsync(cancellationToken);

        ClearRefreshTokenCookie();
        return Ok();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshAsync(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(rawRefreshToken)
            || !TryHashRefreshToken(rawRefreshToken, out var currentTokenHash))
        {
            return Unauthorized();
        }

        var refreshToken = await db.RefreshTokens
            .Include(token => token.User)
            .Where(token => token.TokenHash == currentTokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (refreshToken is null
            || refreshToken.RevokedAt is not null
            || refreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Unauthorized();
        }

        if (await userManager.IsLockedOutAsync(refreshToken.User))
        {
            AddAuditEvent(
                refreshToken.UserId,
                "auth.refresh_forbidden",
                "ApplicationUser",
                refreshToken.UserId,
                null);
            await db.SaveChangesAsync(cancellationToken);
            return Forbid();
        }

        var tokens = await tokenService.CreateTokenPairAsync(refreshToken.User, cancellationToken);
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp = GetClientIp();
        refreshToken.ReplacedByTokenHash = tokenService.HashRefreshToken(tokens.RefreshToken);

        db.RefreshTokens.Add(CreateRefreshToken(refreshToken.UserId, tokens, null));
        AddAuditEvent(
            refreshToken.UserId,
            "auth.refresh_succeeded",
            "ApplicationUser",
            refreshToken.UserId,
            null);

        await db.SaveChangesAsync(cancellationToken);

        SetRefreshTokenCookie(tokens);
        return Ok(ToResponse(refreshToken.User, tokens));
    }

    private static AuthResponse ToResponse(ApplicationUser user, AuthTokenResult tokens)
    {
        return new AuthResponse(
            user.Id,
            user.Email ?? string.Empty,
            "Bearer",
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt);
    }

    private RefreshToken CreateRefreshToken(
        Guid userId,
        AuthTokenResult tokens,
        string? replacedByTokenHash)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenService.HashRefreshToken(tokens.RefreshToken),
            ExpiresAt = tokens.RefreshTokenExpiresAt,
            ReplacedByTokenHash = replacedByTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = GetClientIp()
        };
    }

    private void AddAuditEvent(
        Guid? actorUserId,
        string eventType,
        string? entityType,
        Guid? entityId,
        object? payload)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private bool TryHashRefreshToken(string refreshToken, out string hash)
    {
        try
        {
            hash = tokenService.HashRefreshToken(refreshToken);
            return true;
        }
        catch (FormatException)
        {
            hash = string.Empty;
            return false;
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    private string? GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private void SetRefreshTokenCookie(AuthTokenResult tokens)
    {
        Response.Cookies.Append(
            RefreshTokenCookieName,
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = tokens.RefreshTokenExpiresAt,
                Path = "/api/auth"
            });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth"
            });
    }

    private ActionResult IdentityValidationProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem(ModelState);
    }
}
