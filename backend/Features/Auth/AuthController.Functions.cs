using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using NetTopologySuite.Geometries;

namespace Backend.Features.Auth;

public sealed partial class AuthController
{
    // Supported email locales and fallback (see ResolveEmailLocale).
    private const string DefaultEmailLocale = "en";
    private static readonly string[] _supportedEmailLocales = ["en", "hu"];

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

    private static string BuildVerificationEmailHtml(string confirmationLink, string locale)
        => BuildEmailHtml("VerifyEmail", locale, confirmationLink);

    private static string BuildPasswordResetEmailHtml(string resetLink, string locale)
        => BuildEmailHtml("ResetPassword", locale, resetLink);

    private static string BuildEmailHtml(string templateName, string locale, string link)
    {
        var assembly = typeof(AuthController).Assembly;
        var resourceName = locale == "hu"
            ? $"Backend.Features.Auth.EmailTemplates.{templateName}.hu.html"
            : $"Backend.Features.Auth.EmailTemplates.{templateName}.html";
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, reader.ReadToEnd(), link);
    }

    private static string VerificationEmailSubject(string locale) => locale == "hu"
        ? "2gather – E-mail-cím megerősítése"
        : "2gather - Verify your email address";

    private static string PasswordResetEmailSubject(string locale) => locale == "hu"
        ? "2gather – Új jelszó beállítása"
        : "2gather - Set a new password";

    // Email language mirrors the frontend's locale resolution. The Next.js
    // proxy sets Accept-Language to the user's chosen locale (the NEXT_LOCALE
    // cookie) when present; otherwise the browser's Accept-Language flows
    // through. We best-fit match here and fall back to English.
    private string ResolveEmailLocale()
    {
        var languages = Request.GetTypedHeaders().AcceptLanguage;
        if (languages.Count == 0)
        {
            return DefaultEmailLocale;
        }

        foreach (var language in languages.OrderByDescending(l => l.Quality ?? 1.0))
        {
            var tag = language.Value.ToString().ToLowerInvariant();
            if (tag.Length == 0 || tag == "*")
            {
                continue;
            }

            if (_supportedEmailLocales.Contains(tag))
            {
                return tag;
            }

            var primary = tag.Split('-')[0];
            if (_supportedEmailLocales.Contains(primary))
            {
                return primary;
            }
        }

        return DefaultEmailLocale;
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

    private async Task SendPasswordResetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = $"{emailOptions.Value.FrontendBaseUrl}/reset-password?userId={user.Id}&token={encodedToken}";

        var locale = ResolveEmailLocale();
        await emailSender.SendEmailAsync(
            user.Email!,
            PasswordResetEmailSubject(locale),
            BuildPasswordResetEmailHtml(link, locale),
            cancellationToken);
    }

    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = $"{emailOptions.Value.FrontendBaseUrl}/verify-email?userId={user.Id}&token={encodedToken}";

        var locale = ResolveEmailLocale();
        await emailSender.SendEmailAsync(
            user.Email!,
            VerificationEmailSubject(locale),
            BuildVerificationEmailHtml(link, locale),
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
