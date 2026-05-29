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
        if (exception.InnerException is not PostgresException postgresException)
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

    /// <summary>
    /// Checks if a DbUpdateException is due to a foreign-key constraint violation.
    /// Used when a hard delete is rejected because dependent rows reference the
    /// row being deleted (e.g. tasks still reference the category).
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a foreign-key constraint violation, false otherwise.</returns>
    public static bool IsForeignKeyViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        return postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation;
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate task conversation violation.
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate task conversation violation, false otherwise.</returns>
    public static bool IsDuplicateTaskConversation(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_task_conversations_task_helper");
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate task completion confirmation violation
    /// (same task + same profile already confirmed completion).
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate completion confirmation violation, false otherwise.</returns>
    public static bool IsDuplicateTaskCompletionConfirmation(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_task_completion_confirmations_task_profile");
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate task-completion points reward
    /// (the partial unique index covering <c>EntryType = 'TaskCompletedReward'</c>).
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate completion reward violation, false otherwise.</returns>
    public static bool IsDuplicateTaskCompletionReward(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_points_ledger_task_reward_once");
    }

    /// <summary>
    /// Checks if a DbUpdateException is a duplicate review violation
    /// (same reviewer already submitted a review for the same task).
    /// </summary>
    /// <param name="exception">The DbUpdateException to check.</param>
    /// <returns>True if the exception is a duplicate review violation, false otherwise.</returns>
    public static bool IsDuplicateReview(DbUpdateException exception)
    {
        return IsUniqueConstraintViolation(exception, "ux_reviews_task_reviewer");
    }
}
