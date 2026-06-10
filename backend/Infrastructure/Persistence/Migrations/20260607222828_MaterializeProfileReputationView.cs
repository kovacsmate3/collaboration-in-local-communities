using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterializeProfileReputationView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Promote profile_reputation_v from a plain view (recomputed on every
            // read) to a materialized view that is refreshed on a schedule by
            // ProfileReputationRefreshBackgroundService. The aggregation is
            // unchanged from InitialCreate; only the storage model changes.
            //
            // The unique index on profile_id is required so the background job can
            // run REFRESH MATERIALIZED VIEW CONCURRENTLY (which keeps the view
            // readable while it rebuilds). profile_id is unique because the query
            // selects one row per data.profiles row.
            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS analytics.profile_reputation_v;

                CREATE MATERIALIZED VIEW analytics.profile_reputation_v AS
                WITH review_stats AS (
                    SELECT
                        r.reviewee_profile_id AS profile_id,
                        AVG(r.rating)::numeric(3,2) AS average_rating,
                        COUNT(*)::bigint AS review_count
                    FROM data.reviews r
                    GROUP BY r.reviewee_profile_id
                ),
                completed_task_participants AS (
                    SELECT t.id AS task_id, t.seeker_profile_id AS profile_id
                    FROM data.tasks t
                    WHERE t.status = 'Completed'

                    UNION ALL

                    SELECT t.id AS task_id, t.accepted_helper_profile_id AS profile_id
                    FROM data.tasks t
                    WHERE t.status = 'Completed'
                      AND t.accepted_helper_profile_id IS NOT NULL
                ),
                completed_task_stats AS (
                    SELECT
                        ctp.profile_id,
                        COUNT(DISTINCT ctp.task_id)::bigint AS completed_task_count
                    FROM completed_task_participants ctp
                    GROUP BY ctp.profile_id
                )
                SELECT
                    p.id AS profile_id,
                    COALESCE(rs.average_rating, 0)::numeric(3,2) AS average_rating,
                    COALESCE(rs.review_count, 0)::bigint AS review_count,
                    COALESCE(cts.completed_task_count, 0)::bigint AS completed_task_count
                FROM data.profiles p
                LEFT JOIN review_stats rs ON rs.profile_id = p.id
                LEFT JOIN completed_task_stats cts ON cts.profile_id = p.id
                WITH DATA;

                CREATE UNIQUE INDEX ix_profile_reputation_v_profile_id
                    ON analytics.profile_reputation_v (profile_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the plain view from InitialCreate. Dropping the
            // materialized view also drops its unique index.
            migrationBuilder.Sql("""
                DROP MATERIALIZED VIEW IF EXISTS analytics.profile_reputation_v;

                CREATE OR REPLACE VIEW analytics.profile_reputation_v AS
                WITH review_stats AS (
                    SELECT
                        r.reviewee_profile_id AS profile_id,
                        AVG(r.rating)::numeric(3,2) AS average_rating,
                        COUNT(*)::bigint AS review_count
                    FROM data.reviews r
                    GROUP BY r.reviewee_profile_id
                ),
                completed_task_participants AS (
                    SELECT t.id AS task_id, t.seeker_profile_id AS profile_id
                    FROM data.tasks t
                    WHERE t.status = 'Completed'

                    UNION ALL

                    SELECT t.id AS task_id, t.accepted_helper_profile_id AS profile_id
                    FROM data.tasks t
                    WHERE t.status = 'Completed'
                      AND t.accepted_helper_profile_id IS NOT NULL
                ),
                completed_task_stats AS (
                    SELECT
                        ctp.profile_id,
                        COUNT(DISTINCT ctp.task_id)::bigint AS completed_task_count
                    FROM completed_task_participants ctp
                    GROUP BY ctp.profile_id
                )
                SELECT
                    p.id AS profile_id,
                    COALESCE(rs.average_rating, 0)::numeric(3,2) AS average_rating,
                    COALESCE(rs.review_count, 0)::bigint AS review_count,
                    COALESCE(cts.completed_task_count, 0)::bigint AS completed_task_count
                FROM data.profiles p
                LEFT JOIN review_stats rs ON rs.profile_id = p.id
                LEFT JOIN completed_task_stats cts ON cts.profile_id = p.id;
                """);
        }
    }
}
