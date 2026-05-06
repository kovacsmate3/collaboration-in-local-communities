using Microsoft.AspNetCore.Identity;

namespace Backend.Infrastructure.Persistence.Seeding;

internal static class IdentityResultExtensions
{
    internal static void ThrowIfFailed(this IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException(
            $"Failed to {operation} while seeding development data. {errors}");
    }
}
