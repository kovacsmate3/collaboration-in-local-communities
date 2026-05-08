using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Infrastructure.Validation;

/// <summary>
/// Provides common field validation helpers for API controllers.
/// </summary>
public static class FieldValidator
{
    /// <summary>
    /// Validates that a required field is not empty or whitespace.
    /// </summary>
    /// <param name="modelState">The ModelStateDictionary to add errors to.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is valid (not empty/whitespace), false otherwise.</returns>
    public static bool ValidateRequired(ModelStateDictionary modelState, string fieldName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        modelState.AddModelError(fieldName, $"{fieldName} is required.");
        return false;
    }

    /// <summary>
    /// Checks if a value is empty or whitespace.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is empty or null, false otherwise.</returns>
    public static bool IsEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Validates a string field after trimming, ensuring it meets required/min/max length constraints.
    /// </summary>
    /// <param name="modelState">The ModelStateDictionary to add errors to.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <param name="value">The raw value to trim and validate.</param>
    /// <param name="minimumLength">The minimum length after trimming.</param>
    /// <param name="maximumLength">The maximum length after trimming.</param>
    /// <param name="trimmedValue">The trimmed value if validation succeeds; empty string otherwise.</param>
    /// <returns>True if the trimmed value is valid, false otherwise.</returns>
    public static bool ValidateTrimmedString(
        ModelStateDictionary modelState,
        string fieldName,
        string? value,
        int minimumLength,
        int maximumLength,
        out string trimmedValue)
    {
        trimmedValue = value?.Trim() ?? string.Empty;

        if (!ValidateRequired(modelState, fieldName, trimmedValue))
        {
            return false;
        }

        if (trimmedValue.Length >= minimumLength && trimmedValue.Length <= maximumLength)
        {
            return true;
        }

        modelState.AddModelError(
            fieldName,
            $"The field {fieldName} must be a string with a minimum length of {minimumLength} and a maximum length of {maximumLength}.");
        return false;
    }
}
