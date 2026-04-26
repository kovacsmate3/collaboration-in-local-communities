using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "log");

            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.EnsureSchema(
                name: "data");

            migrationBuilder.EnsureSchema(
                name: "security");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.EnsureSchema(
                name: "tmp");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateSequence(
                name: "task_public_code_seq",
                schema: "data");

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skills", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "terms_versions",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_url = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    workplace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    location_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_profile_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    review_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_task_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "security",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "security",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "security",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_terms_acceptances",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    terms_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_terms_acceptances", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_terms_acceptances_terms_versions_terms_version_id",
                        column: x => x.terms_version_id,
                        principalSchema: "config",
                        principalTable: "terms_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_terms_acceptances_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "security",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_events",
                schema: "log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_activity_events_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_activity_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "security",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profile_privacy_settings",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    show_workplace = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_position = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_location = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_availability = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_privacy_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_profile_privacy_settings_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_skills",
                schema: "data",
                columns: table => new
                {
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profile_skills", x => new { x.profile_id, x.skill_id });
                    table.ForeignKey(
                        name: "fk_profile_skills_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_profile_skills_skills_skill_id",
                        column: x => x.skill_id,
                        principalSchema: "config",
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    public_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValueSql: "('TASK-' || to_char(now(), 'YYYY') || '-' || lpad(nextval('data.task_public_code_seq')::text, 6, '0'))"),
                    seeker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_helper_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    location_text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    compensation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    compensation_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Open"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tasks", x => x.id);
                    table.CheckConstraint("ck_tasks_compensation_amount_non_negative", "compensation_amount IS NULL OR compensation_amount >= 0");
                    table.CheckConstraint("ck_tasks_compensation_type", "compensation_type IN ('Paid', 'Points', 'Voluntary', 'Barter')");
                    table.CheckConstraint("ck_tasks_distinct_profiles", "accepted_helper_profile_id IS NULL OR seeker_profile_id <> accepted_helper_profile_id");
                    table.CheckConstraint("ck_tasks_status", "status IN ('Open', 'InProgress', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "fk_tasks_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "config",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tasks_profiles_accepted_helper_profile_id",
                        column: x => x.accepted_helper_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tasks_profiles_seeker_profile_id",
                        column: x => x.seeker_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "points_ledger",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_points_ledger", x => x.id);
                    table.CheckConstraint("ck_points_ledger_amount_non_zero", "amount <> 0");
                    table.CheckConstraint("ck_points_ledger_entry_type", "entry_type IN ('TaskCompletedReward', 'ManualAdjustment', 'Redemption')");
                    table.ForeignKey(
                        name: "fk_points_ledger_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_points_ledger_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewee_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_distinct_profiles", "reviewer_profile_id <> reviewee_profile_id");
                    table.CheckConstraint("ck_reviews_rating_range", "rating BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_reviews_profiles_reviewee_profile_id",
                        column: x => x.reviewee_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_profiles_reviewer_profile_id",
                        column: x => x.reviewer_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reviews_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_applications",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    helper_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_applications", x => x.id);
                    table.CheckConstraint("ck_task_applications_status", "status IN ('Pending', 'Accepted', 'Rejected', 'Withdrawn')");
                    table.ForeignKey(
                        name: "fk_task_applications_profiles_helper_profile_id",
                        column: x => x.helper_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_applications_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_completion_confirmations",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_completion_confirmations", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_completion_confirmations_profiles_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_completion_confirmations_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_conversations",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seeker_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    helper_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cosmos_conversation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_conversations_profiles_helper_profile_id",
                        column: x => x.helper_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_conversations_profiles_seeker_profile_id",
                        column: x => x.seeker_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_conversations_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_status_history",
                schema: "log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_by_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_task_status_history_profiles_changed_by_profile_id",
                        column: x => x.changed_by_profile_id,
                        principalSchema: "data",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_task_status_history_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "data",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "config",
                table: "categories",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "name", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), "moving", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Moving", 10, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000102"), "tutoring", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Tutoring", 20, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000103"), "repairs", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Repairs", 30, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000104"), "shopping", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Shopping", 40, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000105"), "pet_care", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Pet Care", 50, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000106"), "cleaning", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Cleaning", 60, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000107"), "errands", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Errands", 70, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000108"), "other", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Other", 80, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "config",
                table: "skills",
                columns: new[] { "id", "code", "created_at", "description", "is_active", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000201"), "furniture_assembly", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Furniture Assembly", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000202"), "moving_help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Moving Help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000203"), "tutoring_math", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Math Tutoring", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000204"), "tutoring_programming", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Programming Tutoring", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000205"), "language_tutoring", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Language Tutoring", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000206"), "pet_sitting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Pet Sitting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000207"), "dog_walking", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Dog Walking", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000208"), "shopping_help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Shopping Help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000209"), "cleaning", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Cleaning", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000210"), "minor_repairs", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Minor Repairs", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000211"), "plumbing_basic", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Basic Plumbing", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000212"), "electrical_basic", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Basic Electrical", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000213"), "gardening", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Gardening", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000214"), "cooking", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Cooking", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000215"), "babysitting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Babysitting", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000216"), "elderly_help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Elderly Help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000217"), "transport", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Transport", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000218"), "computer_help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Computer Help", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000219"), "phone_setup", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Phone Setup", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("00000000-0000-0000-0000-000000000220"), "other", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "Other", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "config",
                table: "terms_versions",
                columns: new[] { "id", "content_url", "created_at", "effective_from", "is_active", "title", "updated_at", "version" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000301"), null, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Initial Terms and Conditions", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "1.0" });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_created_at",
                schema: "log",
                table: "activity_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_event_type_created_at",
                schema: "log",
                table: "activity_events",
                columns: new[] { "event_type", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_profile_id_created_at",
                schema: "log",
                table: "activity_events",
                columns: new[] { "profile_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activity_events_user_id_created_at",
                schema: "log",
                table: "activity_events",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_user_id_created_at",
                schema: "log",
                table: "audit_events",
                columns: new[] { "actor_user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_created_at",
                schema: "log",
                table: "audit_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity",
                schema: "log",
                table: "audit_events",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active",
                schema: "config",
                table: "categories",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_categories_sort_order",
                schema: "config",
                table: "categories",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ux_categories_code",
                schema: "config",
                table: "categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_points_ledger_profile_id_created_at",
                schema: "data",
                table: "points_ledger",
                columns: new[] { "profile_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_points_ledger_task_id",
                schema: "data",
                table: "points_ledger",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ux_points_ledger_task_reward_once",
                schema: "data",
                table: "points_ledger",
                columns: new[] { "task_id", "profile_id", "entry_type" },
                unique: true,
                filter: "entry_type = 'TaskCompletedReward'");

            migrationBuilder.CreateIndex(
                name: "ux_profile_privacy_settings_profile_id",
                schema: "data",
                table: "profile_privacy_settings",
                column: "profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_profile_skills_skill_id",
                schema: "data",
                table: "profile_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "ix_profiles_display_name",
                schema: "data",
                table: "profiles",
                column: "display_name");

            migrationBuilder.CreateIndex(
                name: "ix_profiles_location",
                schema: "data",
                table: "profiles",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ux_profiles_user_id",
                schema: "data",
                table: "profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                schema: "security",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "security",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                schema: "security",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reviews_reviewee_profile_id_created_at",
                schema: "data",
                table: "reviews",
                columns: new[] { "reviewee_profile_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_reviews_reviewer_profile_id",
                schema: "data",
                table: "reviews",
                column: "reviewer_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_task_id",
                schema: "data",
                table: "reviews",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ux_reviews_task_reviewer",
                schema: "data",
                table: "reviews",
                columns: new[] { "task_id", "reviewer_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "security",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "role_name_index",
                schema: "security",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skills_is_active",
                schema: "config",
                table: "skills",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_skills_name",
                schema: "config",
                table: "skills",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ux_skills_code",
                schema: "config",
                table: "skills",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_applications_helper_profile_id",
                schema: "data",
                table: "task_applications",
                column: "helper_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_applications_status",
                schema: "data",
                table: "task_applications",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_task_applications_task_id",
                schema: "data",
                table: "task_applications",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ux_task_applications_task_helper",
                schema: "data",
                table: "task_applications",
                columns: new[] { "task_id", "helper_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_completion_confirmations_profile_id",
                schema: "data",
                table: "task_completion_confirmations",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_completion_confirmations_task_id",
                schema: "data",
                table: "task_completion_confirmations",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "ux_task_completion_confirmations_task_profile",
                schema: "data",
                table: "task_completion_confirmations",
                columns: new[] { "task_id", "profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_conversations_helper_profile_id",
                schema: "data",
                table: "task_conversations",
                column: "helper_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_conversations_seeker_profile_id",
                schema: "data",
                table: "task_conversations",
                column: "seeker_profile_id");

            migrationBuilder.CreateIndex(
                name: "ux_task_conversations_cosmos_conversation_id",
                schema: "data",
                table: "task_conversations",
                column: "cosmos_conversation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_task_conversations_task_id",
                schema: "data",
                table: "task_conversations",
                column: "task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_task_status_history_changed_by_profile_id",
                schema: "log",
                table: "task_status_history",
                column: "changed_by_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_task_status_history_task_id_changed_at",
                schema: "log",
                table: "task_status_history",
                columns: new[] { "task_id", "changed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_accepted_helper_profile_id",
                schema: "data",
                table: "tasks",
                column: "accepted_helper_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_category_status",
                schema: "data",
                table: "tasks",
                columns: new[] { "category_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_compensation_status",
                schema: "data",
                table: "tasks",
                columns: new[] { "compensation_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_description_trgm",
                schema: "data",
                table: "tasks",
                column: "description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_location",
                schema: "data",
                table: "tasks",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_open_created_at",
                schema: "data",
                table: "tasks",
                column: "created_at",
                descending: new bool[0],
                filter: "status = 'Open'");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_seeker_profile_id",
                schema: "data",
                table: "tasks",
                column: "seeker_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_status_created_at",
                schema: "data",
                table: "tasks",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_title_trgm",
                schema: "data",
                table: "tasks",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ux_tasks_public_code",
                schema: "data",
                table: "tasks",
                column: "public_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_terms_versions_effective_from",
                schema: "config",
                table: "terms_versions",
                column: "effective_from");

            migrationBuilder.CreateIndex(
                name: "ix_terms_versions_is_active",
                schema: "config",
                table: "terms_versions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_terms_versions_version",
                schema: "config",
                table: "terms_versions",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "security",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "security",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "security",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_terms_acceptances_terms_version_id",
                schema: "data",
                table: "user_terms_acceptances",
                column: "terms_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_terms_acceptances_user_id",
                schema: "data",
                table: "user_terms_acceptances",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_terms_acceptances_user_terms",
                schema: "data",
                table: "user_terms_acceptances",
                columns: new[] { "user_id", "terms_version_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "email_index",
                schema: "security",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "user_name_index",
                schema: "security",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION log.set_updated_at()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    NEW.updated_at = now();
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER trg_categories_set_updated_at
                BEFORE UPDATE ON config.categories
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_skills_set_updated_at
                BEFORE UPDATE ON config.skills
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_terms_versions_set_updated_at
                BEFORE UPDATE ON config.terms_versions
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_profiles_set_updated_at
                BEFORE UPDATE ON data.profiles
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_profile_privacy_settings_set_updated_at
                BEFORE UPDATE ON data.profile_privacy_settings
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_tasks_set_updated_at
                BEFORE UPDATE ON data.tasks
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_task_applications_set_updated_at
                BEFORE UPDATE ON data.task_applications
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE TRIGGER trg_reviews_set_updated_at
                BEFORE UPDATE ON data.reviews
                FOR EACH ROW EXECUTE FUNCTION log.set_updated_at();

                CREATE OR REPLACE VIEW analytics.profile_points_balance_v AS
                SELECT
                    p.id AS profile_id,
                    COALESCE(SUM(pl.amount), 0)::bigint AS points_balance
                FROM data.profiles p
                LEFT JOIN data.points_ledger pl ON pl.profile_id = p.id
                GROUP BY p.id;

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

                CREATE OR REPLACE VIEW analytics.kpi_current_v AS
                WITH metrics AS (
                    SELECT
                        (SELECT COUNT(*)::bigint FROM security.users) AS registered_users,
                        (
                            SELECT COUNT(DISTINCT ae.user_id)::bigint
                            FROM log.activity_events ae
                            WHERE ae.user_id IS NOT NULL
                              AND ae.created_at >= now() - interval '7 days'
                        ) AS active_users_7d,
                        (
                            SELECT COUNT(*)::bigint
                            FROM data.tasks t
                            WHERE t.created_at >= now() - interval '7 days'
                        ) AS tasks_posted_7d,
                        (
                            SELECT COUNT(*)::bigint
                            FROM data.tasks t
                            WHERE t.status = 'Completed'
                              AND t.completed_at >= now() - interval '7 days'
                        ) AS completed_tasks_7d
                )
                SELECT
                    registered_users,
                    active_users_7d,
                    tasks_posted_7d,
                    completed_tasks_7d,
                    CASE
                        WHEN tasks_posted_7d = 0 THEN 0::numeric
                        ELSE ROUND(completed_tasks_7d::numeric / tasks_posted_7d::numeric, 4)
                    END AS completion_rate_7d
                FROM metrics;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS analytics.kpi_current_v;
                DROP VIEW IF EXISTS analytics.profile_reputation_v;
                DROP VIEW IF EXISTS analytics.profile_points_balance_v;

                DROP TRIGGER IF EXISTS trg_categories_set_updated_at ON config.categories;
                DROP TRIGGER IF EXISTS trg_skills_set_updated_at ON config.skills;
                DROP TRIGGER IF EXISTS trg_terms_versions_set_updated_at ON config.terms_versions;
                DROP TRIGGER IF EXISTS trg_profiles_set_updated_at ON data.profiles;
                DROP TRIGGER IF EXISTS trg_profile_privacy_settings_set_updated_at ON data.profile_privacy_settings;
                DROP TRIGGER IF EXISTS trg_tasks_set_updated_at ON data.tasks;
                DROP TRIGGER IF EXISTS trg_task_applications_set_updated_at ON data.task_applications;
                DROP TRIGGER IF EXISTS trg_reviews_set_updated_at ON data.reviews;
                """);

            migrationBuilder.DropTable(
                name: "activity_events",
                schema: "log");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "log");

            migrationBuilder.DropTable(
                name: "points_ledger",
                schema: "data");

            migrationBuilder.DropTable(
                name: "profile_privacy_settings",
                schema: "data");

            migrationBuilder.DropTable(
                name: "profile_skills",
                schema: "data");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "security");

            migrationBuilder.DropTable(
                name: "reviews",
                schema: "data");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "task_applications",
                schema: "data");

            migrationBuilder.DropTable(
                name: "task_completion_confirmations",
                schema: "data");

            migrationBuilder.DropTable(
                name: "task_conversations",
                schema: "data");

            migrationBuilder.DropTable(
                name: "task_status_history",
                schema: "log");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "user_terms_acceptances",
                schema: "data");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "security");

            migrationBuilder.DropTable(
                name: "skills",
                schema: "config");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "data");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "security");

            migrationBuilder.DropTable(
                name: "terms_versions",
                schema: "config");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "config");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "data");

            migrationBuilder.DropTable(
                name: "users",
                schema: "security");

            migrationBuilder.DropSequence(
                name: "task_public_code_seq",
                schema: "data");

            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS log.set_updated_at();
                DROP SCHEMA IF EXISTS tmp;
                DROP SCHEMA IF EXISTS analytics;
                """);
        }
    }
}
