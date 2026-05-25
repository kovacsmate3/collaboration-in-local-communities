namespace Backend.Features.Terms;

public static class TermsVersionParser
{
    public static bool TryParse(string version, out int major, out int minor, out int patch)
    {
        major = 0;
        minor = 0;
        patch = 0;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var parts = version.Trim().Split('.');
        if (parts.Length < 2 || parts.Length > 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out major) || major < 0 ||
            !int.TryParse(parts[1], out minor) || minor < 0)
        {
            return false;
        }

        if (parts.Length == 3)
        {
            if (!int.TryParse(parts[2], out patch) || patch < 0)
            {
                return false;
            }
        }

        return true;
    }

    public static string Format(int major, int minor, int patch) => $"{major}.{minor}.{patch}";
}
