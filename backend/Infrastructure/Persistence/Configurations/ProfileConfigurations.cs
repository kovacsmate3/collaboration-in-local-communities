using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable(TableNames.Profiles, SchemaNames.Data);

        builder.HasGeneratedUuid(profile => profile.Id);
        builder.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.Bio).HasMaxLength(1000);
        builder.Property(profile => profile.Workplace).HasMaxLength(200);
        builder.Property(profile => profile.Position).HasMaxLength(200);
        builder.Property(profile => profile.Availability).HasMaxLength(500);
        builder.Property(profile => profile.PhotoUrl);
        builder.Property(profile => profile.Location).HasColumnType("geography(Point,4326)");
        builder.Property(profile => profile.LocationText).HasMaxLength(300);
        builder.Property(profile => profile.IsProfileCompleted).HasDefaultValue(false);
        builder.Property(profile => profile.AverageRating).HasColumnType("numeric(3,2)").HasDefaultValue(0m);
        builder.Property(profile => profile.ReviewCount).HasDefaultValue(0);
        builder.Property(profile => profile.CompletedTaskCount).HasDefaultValue(0);
        builder.HasCreatedAt(profile => profile.CreatedAt);
        builder.HasUpdatedAt(profile => profile.UpdatedAt);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profile => profile.UserId)
            .IsUnique()
            .HasDatabaseName("ux_profiles_user_id");

        builder.HasIndex(profile => profile.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_profiles_location");

        builder.HasIndex(profile => profile.DisplayName)
            .HasDatabaseName("ix_profiles_display_name");
    }
}

internal sealed class ProfileSkillConfiguration : IEntityTypeConfiguration<ProfileSkill>
{
    public void Configure(EntityTypeBuilder<ProfileSkill> builder)
    {
        builder.ToTable(TableNames.ProfileSkills, SchemaNames.Data);

        builder.HasKey(profileSkill => new { profileSkill.ProfileId, profileSkill.SkillId });
        builder.HasCreatedAt(profileSkill => profileSkill.CreatedAt);

        builder.HasOne(profileSkill => profileSkill.Profile)
            .WithMany(profile => profile.ProfileSkills)
            .HasForeignKey(profileSkill => profileSkill.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(profileSkill => profileSkill.Skill)
            .WithMany(skill => skill.ProfileSkills)
            .HasForeignKey(profileSkill => profileSkill.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profileSkill => profileSkill.SkillId)
            .HasDatabaseName("ix_profile_skills_skill_id");
    }
}

internal sealed class ProfilePrivacySettingsConfiguration : IEntityTypeConfiguration<ProfilePrivacySettings>
{
    public void Configure(EntityTypeBuilder<ProfilePrivacySettings> builder)
    {
        builder.ToTable(TableNames.ProfilePrivacySettings, SchemaNames.Data);

        builder.HasGeneratedUuid(settings => settings.Id);
        builder.Property(settings => settings.ShowWorkplace).HasDefaultValue(true);
        builder.Property(settings => settings.ShowPosition).HasDefaultValue(true);
        builder.Property(settings => settings.ShowLocation).HasDefaultValue(true);
        builder.Property(settings => settings.ShowAvailability).HasDefaultValue(true);
        builder.HasCreatedAt(settings => settings.CreatedAt);
        builder.HasUpdatedAt(settings => settings.UpdatedAt);

        builder.HasOne(settings => settings.Profile)
            .WithOne(profile => profile.PrivacySettings)
            .HasForeignKey<ProfilePrivacySettings>(settings => settings.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(settings => settings.ProfileId)
            .IsUnique()
            .HasDatabaseName("ux_profile_privacy_settings_profile_id");
    }
}

internal sealed class UserTermsAcceptanceConfiguration : IEntityTypeConfiguration<UserTermsAcceptance>
{
    public void Configure(EntityTypeBuilder<UserTermsAcceptance> builder)
    {
        builder.ToTable(TableNames.UserTermsAcceptances, SchemaNames.Data);

        builder.HasGeneratedUuid(acceptance => acceptance.Id);
        builder.HasCreatedAt(acceptance => acceptance.AcceptedAt);
        builder.Property(acceptance => acceptance.IpAddress).HasMaxLength(64);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(acceptance => acceptance.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(acceptance => acceptance.TermsVersion)
            .WithMany(terms => terms.Acceptances)
            .HasForeignKey(acceptance => acceptance.TermsVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(acceptance => new { acceptance.UserId, acceptance.TermsVersionId })
            .IsUnique()
            .HasDatabaseName("ux_user_terms_acceptances_user_terms");

        builder.HasIndex(acceptance => acceptance.UserId)
            .HasDatabaseName("ix_user_terms_acceptances_user_id");

        builder.HasIndex(acceptance => acceptance.TermsVersionId)
            .HasDatabaseName("ix_user_terms_acceptances_terms_version_id");
    }
}
