namespace Backend.Infrastructure.Security;

/// <summary>
/// Exposes the end-user IP address attested by the frontend proxy auth middleware.
/// </summary>
public interface IClientIpAccessor
{
    /// <summary>
    /// Returns the attested client IP for the current request, or the underlying connection
    /// address as a last-resort fallback when no attested value is present.
    /// </summary>
    /// <returns>The client IP, or <see langword="null"/> when neither source is available.</returns>
    string? GetClientIp();
}

internal sealed class ClientIpAccessor(IHttpContextAccessor httpContextAccessor, ILogger<ClientIpAccessor> logger) : IClientIpAccessor
{
    internal const string HttpContextItemKey = "ClientIp";

    public string? GetClientIp()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (context.Items.TryGetValue(HttpContextItemKey, out var value) && value is string attestedIp)
        {
            return attestedIp;
        }

        // No attested IP available. In production this means the frontend-proxy auth
        // middleware did not run, or it was misconfigured, so the connection address
        // is the egress IP of whatever called us, not the end user. Log it and fall
        // back so we record something rather than nothing.
        var fallback = context.Connection.RemoteIpAddress?.ToString();
        logger.LogWarning(
            "No attested client IP on HttpContext.Items[\"{Key}\"]; falling back to Connection.RemoteIpAddress = {Fallback}.",
            HttpContextItemKey,
            fallback ?? "<null>");
        return fallback;
    }
}
