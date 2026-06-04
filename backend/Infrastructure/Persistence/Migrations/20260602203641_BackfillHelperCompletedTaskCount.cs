using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillHelperCompletedTaskCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CompletedTaskCount was never maintained before this change, so it
            // reads 0 for everyone. Seed it from existing history: a profile's
            // completed-task count is the number of tasks it completed AS THE
            // ACCEPTED HELPER (the party that performed the work). Seeker-side
            // completions are intentionally excluded so posting tasks does not
            // inflate reputation, matching the runtime increment in
            // TaskCompletionController.ApproveAsync.
            migrationBuilder.Sql(
                """
                UPDATE data.profiles AS p
                SET completed_task_count = sub.completed_count
                FROM (
                    SELECT accepted_helper_profile_id AS profile_id,
                           COUNT(*) AS completed_count
                    FROM data.tasks
                    WHERE status = 'Completed'
                      AND accepted_helper_profile_id IS NOT NULL
                    GROUP BY accepted_helper_profile_id
                ) AS sub
                WHERE p.id = sub.profile_id
                  AND p.completed_task_count <> sub.completed_count;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting this migration restores the pre-backfill state in which
            // the column was never populated (uniformly 0).
            migrationBuilder.Sql(
                """
                UPDATE data.profiles
                SET completed_task_count = 0
                WHERE completed_task_count <> 0;
                """);
        }
    }
}
