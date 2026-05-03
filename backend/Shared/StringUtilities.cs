using System.Diagnostics.CodeAnalysis;

namespace Backend.Shared;

/// <summary>
/// Utilities for normalizing string values.
/// </summary>
public static class StringUtilities
{
    /// <summary>
    /// Normalizes a (nullable) string by trimming whitespace, returning null if empty.
    /// </summary>
    /// <param name="value">The string to normalize, or null.</param>
    /// <returns>The trimmed string, or null if the result would be empty.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
