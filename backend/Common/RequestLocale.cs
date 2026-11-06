namespace Backend.Common;

/// <summary>
/// Resolves the UI locale a request was made in.
/// </summary>
/// <remarks>
/// Mirrors the frontend's locale resolution (<c>frontend/i18n/request.ts</c>).
/// The Next.js proxy sets <c>Accept-Language</c> to the user's chosen locale
/// (the <c>NEXT_LOCALE</c> cookie) when present; otherwise the browser's
/// <c>Accept-Language</c> flows through. We best-fit match against the
/// supported locales and fall back to English.
/// </remarks>
public static class RequestLocale
{
    /// <summary>
    /// The locale used when the request does not ask for a supported one.
    /// </summary>
    public const string Default = "en";

    private static readonly string[] _supported = ["en", "hu"];

    /// <summary>
    /// Returns the best supported locale (<c>en</c> or <c>hu</c>) for the request.
    /// </summary>
    /// <param name="request">The incoming HTTP request.</param>
    /// <returns>A supported locale code; <see cref="Default"/> when none matches.</returns>
    public static string Resolve(HttpRequest request)
    {
        var languages = request.GetTypedHeaders().AcceptLanguage;
        if (languages.Count == 0)
        {
            return Default;
        }

        foreach (var language in languages.OrderByDescending(l => l.Quality ?? 1.0))
        {
            var tag = language.Value.ToString().ToLowerInvariant();
            if (tag.Length == 0 || tag == "*")
            {
                continue;
            }

            if (_supported.Contains(tag))
            {
                return tag;
            }

            var primary = tag.Split('-')[0];
            if (_supported.Contains(primary))
            {
                return primary;
            }
        }

        return Default;
    }
}
