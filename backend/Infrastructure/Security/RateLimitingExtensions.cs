using System.Threading.RateLimiting;

namespace Backend.Infrastructure.Security;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string ConversationsPolicy = "conversations";
    public const string ReviewsPolicy = "reviews";
    public const string PhotoUploadPolicy = "photo-upload";

    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                   ?? new RateLimitingOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.AddPolicy(AuthPolicy, context =>
            {
                var ip = context.RequestServices.GetRequiredService<IClientIpAccessor>().GetClientIp() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.Auth.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.Auth.WindowSeconds)
                });
            });

            limiterOptions.AddPolicy(ConversationsPolicy, context =>
            {
                var ip = context.RequestServices.GetRequiredService<IClientIpAccessor>().GetClientIp() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.Conversations.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.Conversations.WindowSeconds)
                });
            });

            limiterOptions.AddPolicy(ReviewsPolicy, context =>
            {
                var ip = context.RequestServices.GetRequiredService<IClientIpAccessor>().GetClientIp() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.Reviews.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.Reviews.WindowSeconds)
                });
            });

            limiterOptions.AddPolicy(PhotoUploadPolicy, context =>
            {
                var ip = context.RequestServices.GetRequiredService<IClientIpAccessor>().GetClientIp() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = opts.PhotoUpload.PermitLimit,
                    Window = TimeSpan.FromSeconds(opts.PhotoUpload.WindowSeconds)
                });
            });
        });

        return services;
    }
}
