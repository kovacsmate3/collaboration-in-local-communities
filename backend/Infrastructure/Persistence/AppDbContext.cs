using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence.Analytics;
using Backend.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<TermsVersion> TermsVersions => Set<TermsVersion>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<ProfileSkill> ProfileSkills => Set<ProfileSkill>();
    public DbSet<ProfilePrivacySettings> ProfilePrivacySettings => Set<ProfilePrivacySettings>();
    public DbSet<UserTermsAcceptance> UserTermsAcceptances => Set<UserTermsAcceptance>();
    public DbSet<CommunityTask> Tasks => Set<CommunityTask>();
    public DbSet<TaskApplication> TaskApplications => Set<TaskApplication>();
    public DbSet<TaskCompletionConfirmation> TaskCompletionConfirmations => Set<TaskCompletionConfirmation>();
    public DbSet<TaskConversation> TaskConversations => Set<TaskConversation>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PointsLedgerEntry> PointsLedger => Set<PointsLedgerEntry>();
    public DbSet<TaskStatusHistoryEntry> TaskStatusHistory => Set<TaskStatusHistoryEntry>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ProfilePointsBalance> ProfilePointsBalances => Set<ProfilePointsBalance>();
    public DbSet<ProfileReputation> ProfileReputations => Set<ProfileReputation>();
    public DbSet<KpiCurrent> KpiCurrent => Set<KpiCurrent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("pgcrypto");
        builder.HasPostgresExtension("postgis");
        builder.HasPostgresExtension("pg_trgm");

        builder.HasSequence<long>("task_public_code_seq", SchemaNames.Data)
            .StartsAt(1)
            .IncrementsBy(1);

        ConfigureIdentityTables(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureAnalyticsViews(builder);
        builder.ApplySnakeCaseNames();
    }

    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users", SchemaNames.Security);
            entity.Property(user => user.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles", SchemaNames.Security);
            entity.Property(role => role.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .ValueGeneratedOnAdd();

            entity.HasData(
                CreateSeedRole(
                    new Guid("ba1e8ed9-809e-4a1a-a4b8-e3a80c87cf8f"),
                    ApplicationRoleNames.Admin),
                CreateSeedRole(
                    new Guid("da637a4d-0e15-4e7d-977a-b978362e20bb"),
                    ApplicationRoleNames.User));
        });

        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("user_roles", SchemaNames.Security);

        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("user_claims", SchemaNames.Security);

        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("role_claims", SchemaNames.Security);

        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("user_logins", SchemaNames.Security);

        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("user_tokens", SchemaNames.Security);
    }

    private static ApplicationRole CreateSeedRole(Guid id, string name)
    {
        return new ApplicationRole
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = id.ToString()
        };
    }

    private static void ConfigureAnalyticsViews(ModelBuilder builder)
    {
        builder.Entity<ProfilePointsBalance>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("profile_points_balance_v", SchemaNames.Analytics);
        });

        builder.Entity<ProfileReputation>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("profile_reputation_v", SchemaNames.Analytics);
        });

        builder.Entity<KpiCurrent>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("kpi_current_v", SchemaNames.Analytics);
        });
    }
}
