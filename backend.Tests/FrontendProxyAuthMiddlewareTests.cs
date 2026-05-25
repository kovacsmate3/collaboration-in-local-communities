using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace backend.Tests;

public sealed class FrontendProxyAuthMiddlewareTests
{
    private const string SigningKey = "test-signing-key-must-be-at-least-32-bytes-long";

    [Fact]
    public async Task BypassesInDevelopmentEvenWithoutToken()
    {
        var middleware = Build(env: Environments.Development);
        var context = NewContext("POST", "/api/auth/login");

        await middleware.InvokeAsync(context);

        Assert.NotEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(context.Items.ContainsKey("ClientIp"));
    }

    [Fact]
    public async Task BypassesHealthEndpointInProduction()
    {
        var middleware = Build();
        var context = NewContext("GET", "/health");

        await middleware.InvokeAsync(context);

        Assert.NotEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task AcceptsValidTokenAndStashesIp()
    {
        var fixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var middleware = Build(time: new FixedTimeProvider(fixedNow));
        var token = MakeToken(fixedNow, "203.0.113.7", "POST", "/api/auth/login");
        var context = NewContext("POST", "/api/auth/login");
        context.Request.Headers["X-Frontend-Auth"] = token;

        await middleware.InvokeAsync(context);

        Assert.NotEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("203.0.113.7", context.Items["ClientIp"]);
    }

    [Fact]
    public async Task RejectsMissingToken()
    {
        var middleware = Build();
        var context = NewContext("POST", "/api/auth/login");

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task RejectsExpiredToken()
    {
        var fixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var middleware = Build(time: new FixedTimeProvider(fixedNow));
        var staleIssuedAt = fixedNow.AddMinutes(-5);
        var token = MakeToken(staleIssuedAt, "203.0.113.7", "POST", "/api/auth/login");
        var context = NewContext("POST", "/api/auth/login");
        context.Request.Headers["X-Frontend-Auth"] = token;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task RejectsPathMismatch()
    {
        var fixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var middleware = Build(time: new FixedTimeProvider(fixedNow));
        var token = MakeToken(fixedNow, "203.0.113.7", "POST", "/api/auth/login");
        var context = NewContext("POST", "/api/admin/categories");
        context.Request.Headers["X-Frontend-Auth"] = token;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task RejectsMethodMismatch()
    {
        var fixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var middleware = Build(time: new FixedTimeProvider(fixedNow));
        var token = MakeToken(fixedNow, "203.0.113.7", "POST", "/api/auth/login");
        var context = NewContext("DELETE", "/api/auth/login");
        context.Request.Headers["X-Frontend-Auth"] = token;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task RejectsTamperedSignature()
    {
        var fixedNow = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var middleware = Build(time: new FixedTimeProvider(fixedNow));
        var token = MakeToken(fixedNow, "203.0.113.7", "POST", "/api/auth/login");
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');
        var context = NewContext("POST", "/api/auth/login");
        context.Request.Headers["X-Frontend-Auth"] = tampered;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task BypassesWhenDisabled()
    {
        var options = new FrontendProxyAuthOptions { Enabled = false, SigningKey = SigningKey };
        var middleware = new FrontendProxyAuthMiddleware(
            _ => Task.CompletedTask,
            Options.Create(options) is var opts ? new StubOptionsMonitor(opts) : null!,
            new StubHostEnvironment(Environments.Production),
            TimeProvider.System,
            NullLogger<FrontendProxyAuthMiddleware>.Instance);
        var context = NewContext("POST", "/api/auth/login");

        await middleware.InvokeAsync(context);

        Assert.NotEqual(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    private static FrontendProxyAuthMiddleware Build(
        string? env = null,
        TimeProvider? time = null)
    {
        var options = new FrontendProxyAuthOptions { Enabled = true, SigningKey = SigningKey };
        var middleware = new FrontendProxyAuthMiddleware(
            _ => Task.CompletedTask,
            new StubOptionsMonitor(Options.Create(options)),
            new StubHostEnvironment(env ?? Environments.Production),
            time ?? TimeProvider.System,
            NullLogger<FrontendProxyAuthMiddleware>.Instance);
        return middleware;
    }

    private static DefaultHttpContext NewContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    private static string MakeToken(DateTimeOffset issuedAt, string ip, string method, string path)
    {
        var headerJson = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        var payloadObject = new
        {
            iat = issuedAt.ToUnixTimeSeconds(),
            exp = issuedAt.AddSeconds(30).ToUnixTimeSeconds(),
            ip,
            mth = method,
            pth = path,
        };
        var payloadJson = JsonSerializer.Serialize(payloadObject);

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signingInput = $"{headerB64}.{payloadB64}";
        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(SigningKey), Encoding.UTF8.GetBytes(signingInput));
        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        Span<char> buffer = stackalloc char[Base64Url.GetEncodedLength(bytes.Length)];
        Base64Url.EncodeToChars(bytes, buffer, out _, out var written);
        return new string(buffer[..written]);
    }

    private sealed class StubOptionsMonitor(IOptions<FrontendProxyAuthOptions> options) : IOptionsMonitor<FrontendProxyAuthOptions>
    {
        public FrontendProxyAuthOptions CurrentValue => options.Value;

        public FrontendProxyAuthOptions Get(string? name) => options.Value;

        public IDisposable? OnChange(Action<FrontendProxyAuthOptions, string?> listener) => null;
    }

    private sealed class StubHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
