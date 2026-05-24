using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Infrastructure.Security;

public static class FrontendProxyAuthExtensions
{
    public static IServiceCollection AddFrontendProxyAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<FrontendProxyAuthOptions>()
            .Bind(configuration.GetSection(FrontendProxyAuthOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddSingleton<IClientIpAccessor, ClientIpAccessor>();
        return services;
    }

    public static IApplicationBuilder UseFrontendProxyAuth(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FrontendProxyAuthMiddleware>();
    }
}
