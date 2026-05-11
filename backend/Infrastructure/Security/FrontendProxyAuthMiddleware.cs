using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Security;

public sealed class FrontendProxyAuthMiddleware(
    RequestDelegate next,
    IOptionsMonitor<FrontendProxyAuthOptions> options,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<FrontendProxyAuthMiddleware> logger)
{
    private const string HeaderName = "X-Frontend-Auth";
    private const string HealthPath = "/health";

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        var currentOptions = options.CurrentValue;

        if (ShouldBypass(context, currentOptions))
        {
            await next(context);
            return;
        }

        if (!TryValidate(context, currentOptions, out var clientIp, out var failureReason))
        {
            logger.LogWarning(
                "Rejected request {Method} {Path} from {RemoteIp}: {Reason}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress,
                failureReason);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Items[ClientIpAccessor.HttpContextItemKey] = clientIp;
        await next(context);
    }

    private static bool VerifySignature(ReadOnlySpan<char> signingInput, ReadOnlySpan<char> providedSignature, string signingKey)
    {
        Span<byte> expected = stackalloc byte[HMACSHA256.HashSizeInBytes];
        var inputBytes = Encoding.UTF8.GetBytes(signingInput.ToString());
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);

        if (!HMACSHA256.TryHashData(keyBytes, inputBytes, expected, out var written) || written != expected.Length)
        {
            return false;
        }

        Span<byte> provided = stackalloc byte[HMACSHA256.HashSizeInBytes];
        if (!TryBase64UrlDecode(providedSignature, provided, out var providedLength) || providedLength != expected.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private static bool TryDecodePayload(ReadOnlySpan<char> payloadSegment, out ProxyTokenPayload payload)
    {
        payload = default!;

        var buffer = new byte[payloadSegment.Length];
        if (!TryBase64UrlDecode(payloadSegment, buffer, out var decodedLength))
        {
            return false;
        }

        try
        {
            payload = JsonSerializer.Deserialize<ProxyTokenPayload>(buffer.AsSpan(0, decodedLength), _serializerOptions)!;
            return payload is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryBase64UrlDecode(ReadOnlySpan<char> input, Span<byte> destination, out int bytesWritten)
    {
        var status = Base64Url.DecodeFromChars(input, destination, out _, out bytesWritten);
        return status == System.Buffers.OperationStatus.Done;
    }

    private bool ShouldBypass(HttpContext context, FrontendProxyAuthOptions currentOptions)
    {
        if (!currentOptions.Enabled)
        {
            return true;
        }

        if (environment.IsDevelopment())
        {
            return true;
        }

        if (context.Request.Path.Equals(HealthPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool TryValidate(
        HttpContext context,
        FrontendProxyAuthOptions currentOptions,
        out string? clientIp,
        out string failureReason)
    {
        clientIp = null;

        if (string.IsNullOrEmpty(currentOptions.SigningKey))
        {
            failureReason = "signing key not configured";
            return false;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValues)
            || headerValues.Count != 1
            || string.IsNullOrEmpty(headerValues[0]))
        {
            failureReason = "missing or malformed auth header";
            return false;
        }

        var token = headerValues[0]!;
        var firstDot = token.IndexOf('.');
        var lastDot = token.LastIndexOf('.');
        if (firstDot <= 0 || lastDot <= firstDot || lastDot == token.Length - 1)
        {
            failureReason = "token structure invalid";
            return false;
        }

        var signingInput = token.AsSpan(0, lastDot);
        var providedSignature = token.AsSpan(lastDot + 1);

        if (!VerifySignature(signingInput, providedSignature, currentOptions.SigningKey!))
        {
            failureReason = "signature mismatch";
            return false;
        }

        if (!TryDecodePayload(token.AsSpan(firstDot + 1, lastDot - firstDot - 1), out var payload))
        {
            failureReason = "payload decode failed";
            return false;
        }

        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();
        var skew = currentOptions.ClockSkewSeconds;

        if (payload.ExpiresAt + skew < now)
        {
            failureReason = "token expired";
            return false;
        }

        if (payload.IssuedAt - skew > now)
        {
            failureReason = "token issued in the future";
            return false;
        }

        if (payload.ExpiresAt - payload.IssuedAt > currentOptions.MaxTokenLifetimeSeconds)
        {
            failureReason = "token lifetime exceeds policy";
            return false;
        }

        if (!string.Equals(payload.Method, context.Request.Method, StringComparison.OrdinalIgnoreCase))
        {
            failureReason = "method mismatch";
            return false;
        }

        if (!string.Equals(payload.Path, context.Request.Path.Value, StringComparison.Ordinal))
        {
            failureReason = "path mismatch";
            return false;
        }

        if (string.IsNullOrEmpty(payload.Ip))
        {
            failureReason = "missing ip claim";
            return false;
        }

        clientIp = payload.Ip;
        failureReason = string.Empty;
        return true;
    }

    private sealed record ProxyTokenPayload(
        [property: JsonPropertyName("iat")] long IssuedAt,
        [property: JsonPropertyName("exp")] long ExpiresAt,
        [property: JsonPropertyName("ip")] string Ip,
        [property: JsonPropertyName("mth")] string Method,
        [property: JsonPropertyName("pth")] string Path);
}
