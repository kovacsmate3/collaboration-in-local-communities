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
}
