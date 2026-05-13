using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Backend.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IdentityBuilder AddApplicationIdentity(this IServiceCollection services)
    {
        return services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();
    }
}
