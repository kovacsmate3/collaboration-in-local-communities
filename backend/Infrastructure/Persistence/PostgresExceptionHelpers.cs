using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Backend.Infrastructure.Persistence;

/// <summary>
/// Helper methods for handling PostgreSQL-specific exceptions.
/// </summary>
public static class PostgresExceptionHelpers
{
    /// <summary>
    /// Checks if a DbUpdateException is due to a unique constraint violation.
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <param name="constraintName">The name of the constraint to match (optional).</param>
    /// <returns>True if the exception is a unique constraint violation, false otherwise.</returns>
    public static bool IsUniqueConstraintViolation(DbUpdateException exception, string? constraintName = null)
    {
        if (exception?.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        var isUnique = postgresException.SqlState == PostgresErrorCodes.UniqueViolation;

        if (constraintName is null)
        {
            return isUnique;
        }

        return isUnique && postgresException.ConstraintName == constraintName;
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate category code violation.
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate category code violation, false otherwise.</returns>
    public static bool IsDuplicateCategoryCode(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_categories_code");
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate user terms acceptance violation.
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate user terms acceptance violation, false otherwise.</returns>
    public static bool IsDuplicateUserTermsAcceptance(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_user_terms_acceptances_user_terms");
    }
}
