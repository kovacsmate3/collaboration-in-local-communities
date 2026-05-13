using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NetTopologySuite.Geometries;

namespace Backend.Features.Auth;

public sealed partial class AuthController
{
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

    private static string BuildVerificationEmailHtml(string confirmationLink)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8" /></head>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">
              <h2>Verify your email address</h2>
              <p>Welcome to 2gather! Please click the button below to verify your email address and activate your account.</p>
              <p style="margin: 32px 0;">
                <a href="{confirmationLink}"
                   style="background-color: #2563eb; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;">
                  Verify Email
                </a>
              </p>
              <p style="color: #666; font-size: 14px;">
                Or copy and paste this link into your browser:<br />
                <a href="{confirmationLink}" style="color: #2563eb;">{confirmationLink}</a>
              </p>
              <p style="color: #666; font-size: 14px;">This link expires in 24 hours. If you did not create an account, you can safely ignore this email.</p>
            </body>
            </html>
            """;
    }

    private void SetTokenResponseHeaders()
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
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
        return clientIpAccessor.GetClientIp();
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

                // Lax (not Strict) so the refresh cookie is sent on top-level cross-site
                // navigations into the app (e.g. clicking a link from email back into the
                // site). Refresh requests themselves are explicit POSTs from same-origin
                // application code; combined with cookie rotation and the opaque token
                // value this keeps the CSRF surface acceptable while avoiding the
                // spurious re-login UX of Strict.
                SameSite = SameSiteMode.Lax,
                Expires = tokens.RefreshTokenExpiresAt,

                // Path "/" (not "/api/auth") so the Next.js edge middleware can see the
                // cookie and gate protected pages without an extra round-trip. The
                // cookie remains HttpOnly so it is still inaccessible to JavaScript.
                Path = "/"
            });
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            new CookieOptions
            {
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
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

    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = $"{emailOptions.Value.FrontendBaseUrl}/verify-email?userId={user.Id}&token={encodedToken}";

        await emailSender.SendEmailAsync(
            user.Email!,
            "2gather - Verify your email address",
            BuildVerificationEmailHtml(link),
            cancellationToken);
    }

    private bool TryBuildLocation(double? latitude, double? longitude, out Point? location)
    {
        location = null;

        if (latitude.HasValue != longitude.HasValue)
        {
            ModelState.AddModelError(nameof(RegisterRequest.Latitude), "Both Latitude and Longitude must be provided together.");
            ModelState.AddModelError(nameof(RegisterRequest.Longitude), "Both Latitude and Longitude must be provided together.");
            return false;
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            return true;
        }

        if (!double.IsFinite(latitude.Value) || latitude.Value is < -90 or > 90)
        {
            ModelState.AddModelError(nameof(RegisterRequest.Latitude), "Latitude must be between -90 and 90.");
            return false;
        }

        if (!double.IsFinite(longitude.Value) || longitude.Value is < -180 or > 180)
        {
            ModelState.AddModelError(nameof(RegisterRequest.Longitude), "Longitude must be between -180 and 180.");
            return false;
        }

        location = new Point(longitude.Value, latitude.Value) { SRID = 4326 };
        return true;
    }
}
