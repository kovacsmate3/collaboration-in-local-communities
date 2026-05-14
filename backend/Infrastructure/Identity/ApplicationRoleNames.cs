namespace Backend.Infrastructure.Identity;

public static class ApplicationRoleNames
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Admin,
        User
    };
}
