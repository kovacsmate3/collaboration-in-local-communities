using System.Net;
using Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace backend.Tests;

public sealed class RateLimitingTests
{
    [Theory]
    [InlineData(RateLimitingExtensions.AuthPolicy, "Auth")]
    [InlineData(RateLimitingExtensions.ConversationsPolicy, "Conversations")]
    [InlineData(RateLimitingExtensions.ReviewsPolicy, "Reviews")]
    public async Task ExceedingPermitLimit_Returns429(string policy, string configKey)
    {
        const int permitLimit = 3;
        var ct = TestContext.Current.CancellationToken;
        using var host = await BuildHostAsync(policy, configKey, permitLimit, ct);
        var client = host.GetTestClient();

        for (var i = 0; i < permitLimit; i++)
        {
            var response = await client.GetAsync("/probe", ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var overLimit = await client.GetAsync("/probe", ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, overLimit.StatusCode);
    }

    private static async Task<IHost> BuildHostAsync(string policy, string configKey, int permitLimit, CancellationToken cancellationToken = default)
    {
        var inlineConfig = new Dictionary<string, string?>
        {
            [$"{RateLimitingOptions.SectionName}:{configKey}:PermitLimit"] = permitLimit.ToString(),
            [$"{RateLimitingOptions.SectionName}:{configKey}:WindowSeconds"] = "60",
        };

        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureAppConfiguration(cfg => cfg.AddInMemoryCollection(inlineConfig));
                web.ConfigureServices((ctx, services) =>
                {
                    services.AddHttpContextAccessor();
                    services.AddSingleton<IClientIpAccessor>(new StubClientIpAccessor());
                    services.AddRouting();
                    services.AddAppRateLimiting(ctx.Configuration);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseRateLimiter();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/probe", () => Results.Ok())
                            .RequireRateLimiting(policy);
                    });
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);
        return host;
    }

    private sealed class StubClientIpAccessor : IClientIpAccessor
    {
        public string? GetClientIp() => "203.0.113.1";
    }
}
