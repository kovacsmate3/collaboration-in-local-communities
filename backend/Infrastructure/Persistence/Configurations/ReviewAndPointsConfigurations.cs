using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

internal sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable(
            TableNames.Reviews,
            SchemaNames.Data,
            table =>
            {
                table.HasCheckConstraint("ck_reviews_rating_range", "rating BETWEEN 1 AND 5");
                table.HasCheckConstraint("ck_reviews_distinct_profiles", "reviewer_profile_id <> reviewee_profile_id");
            });

        builder.HasGeneratedUuid(review => review.Id);
        builder.Property(review => review.Rating).IsRequired();
        builder.Property(review => review.Comment).HasMaxLength(2000);
        builder.HasCreatedAt(review => review.CreatedAt);
        builder.HasUpdatedAt(review => review.UpdatedAt);

        builder.HasOne(review => review.Task)
            .WithMany(task => task.Reviews)
            .HasForeignKey(review => review.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(review => review.ReviewerProfile)
            .WithMany(profile => profile.ReviewsWritten)
            .HasForeignKey(review => review.ReviewerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(review => review.RevieweeProfile)
            .WithMany(profile => profile.ReviewsReceived)
            .HasForeignKey(review => review.RevieweeProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(review => new { review.TaskId, review.ReviewerProfileId })
            .IsUnique()
            .HasDatabaseName("ux_reviews_task_reviewer");

        builder.HasIndex(review => review.TaskId)
            .HasDatabaseName("ix_reviews_task_id");

        builder.HasIndex(review => review.ReviewerProfileId)
            .HasDatabaseName("ix_reviews_reviewer_profile_id");

        builder.HasIndex(review => new { review.RevieweeProfileId, review.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_reviews_reviewee_profile_id_created_at");
    }
}

internal sealed class PointsLedgerEntryConfiguration : IEntityTypeConfiguration<PointsLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointsLedgerEntry> builder)
    {
        builder.ToTable(
            TableNames.PointsLedger,
            SchemaNames.Data,
            table =>
            {
                table.HasCheckConstraint("ck_points_ledger_amount_non_zero", "amount <> 0");
                table.HasCheckConstraint(
                    "ck_points_ledger_entry_type",
                    ConfigurationHelpers.EnumValuesCheck<PointEntryType>("entry_type"));
            });

        builder.HasGeneratedUuid(entry => entry.Id);
        builder.Property(entry => entry.EntryType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(entry => entry.Description).HasMaxLength(500);
        builder.HasCreatedAt(entry => entry.CreatedAt);

        builder.HasOne(entry => entry.Profile)
            .WithMany(profile => profile.PointsLedgerEntries)
            .HasForeignKey(entry => entry.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.Task)
            .WithMany(task => task.PointsLedgerEntries)
            .HasForeignKey(entry => entry.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.ProfileId, entry.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_points_ledger_profile_id_created_at");

        builder.HasIndex(entry => entry.TaskId)
            .HasDatabaseName("ix_points_ledger_task_id");

        builder.HasIndex(entry => new { entry.TaskId, entry.ProfileId, entry.EntryType })
            .IsUnique()
            .HasFilter("entry_type = 'TaskCompletedReward'")
            .HasDatabaseName("ux_points_ledger_task_reward_once");
    }
}
