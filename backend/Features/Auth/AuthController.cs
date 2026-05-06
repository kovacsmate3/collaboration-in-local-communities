using Backend.Common;
using Backend.Domain.Entities;
using Backend.Features.Terms;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed partial class AuthController(
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
            .GetCurrentAsync(now, cancellationToken);

        if (activeTerms is not null)
        {
            db.UserTermsAcceptances.Add(new UserTermsAcceptance
            {
                Id = Guid.NewGuid(),
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
        SetTokenResponseHeaders();
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
        SetTokenResponseHeaders();
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

        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(rawRefreshToken)
            && TryHashRefreshToken(rawRefreshToken, out var refreshTokenHash))
        {
            var refreshToken = await db.RefreshTokens
                .Where(token => token.UserId == userId.Value && token.TokenHash == refreshTokenHash)
                .FirstOrDefaultAsync(cancellationToken);

            if (refreshToken is not null && refreshToken.RevokedAt is null)
            {
                refreshToken.RevokedAt = DateTimeOffset.UtcNow;
                refreshToken.RevokedByIp = GetClientIp();
            }
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
        var revokedAt = DateTimeOffset.UtcNow;
        var clientIp = GetClientIp();
        var replacedByHash = tokenService.HashRefreshToken(tokens.RefreshToken);

        // Atomic revocation: only one concurrent request can win the WHERE revoked_at IS NULL predicate.
        // PostgreSQL re-checks the predicate after acquiring the row lock, so rowsRevoked == 0 means
        // another request already revoked this token in the race window.
        var rowsRevoked = await db.RefreshTokens
            .Where(t => t.TokenHash == currentTokenHash && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.RevokedAt, revokedAt)
                    .SetProperty(t => t.RevokedByIp, clientIp)
                    .SetProperty(t => t.ReplacedByTokenHash, replacedByHash),
                cancellationToken);

        if (rowsRevoked == 0)
        {
            return Unauthorized();
        }

        db.RefreshTokens.Add(CreateRefreshToken(refreshToken.UserId, tokens, null));
        AddAuditEvent(
            refreshToken.UserId,
            "auth.refresh_succeeded",
            "ApplicationUser",
            refreshToken.UserId,
            null);

        await db.SaveChangesAsync(cancellationToken);

        SetRefreshTokenCookie(tokens);
        SetTokenResponseHeaders();
        return Ok(ToResponse(refreshToken.User, tokens));
    }
}
