using System.Threading.RateLimiting;

namespace Backend.Infrastructure.Security;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string ConversationsPolicy = "conversations";
    public const string ReviewsPolicy = "reviews";
    public const string PhotoUploadPolicy = "photo-upload";
    public const string TasksReadPolicy = "tasks-read";
    public const string TasksWritePolicy = "tasks-write";
    public const string LocationsPolicy = "locations";
    public const string AdminPolicy = "admin";

    public static IServiceCollection AddAppRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                   ?? new RateLimitingOptions();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddIpFixedWindowPolicy(limiterOptions, AuthPolicy, opts.Auth);
            AddIpFixedWindowPolicy(limiterOptions, ConversationsPolicy, opts.Conversations);
            AddIpFixedWindowPolicy(limiterOptions, ReviewsPolicy, opts.Reviews);
            AddIpFixedWindowPolicy(limiterOptions, PhotoUploadPolicy, opts.PhotoUpload);
            AddIpFixedWindowPolicy(limiterOptions, TasksReadPolicy, opts.TasksRead);
            AddIpFixedWindowPolicy(limiterOptions, TasksWritePolicy, opts.TasksWrite);
            AddIpFixedWindowPolicy(limiterOptions, LocationsPolicy, opts.Locations);
            AddIpFixedWindowPolicy(limiterOptions, AdminPolicy, opts.Admin);
        });

        return services;
    }

    private static void AddIpFixedWindowPolicy(
        Microsoft.AspNetCore.RateLimiting.RateLimiterOptions limiterOptions,
        string policyName,
        WindowPolicyOptions policy)
    {
        limiterOptions.AddPolicy(policyName, context =>
        {
            var ip = context.RequestServices.GetRequiredService<IClientIpAccessor>().GetClientIp() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds)
            });
        });
    }
}
