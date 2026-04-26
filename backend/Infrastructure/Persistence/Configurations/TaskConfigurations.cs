using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Infrastructure.Persistence.Configurations;

internal sealed class CommunityTaskConfiguration : IEntityTypeConfiguration<CommunityTask>
{
    public void Configure(EntityTypeBuilder<CommunityTask> builder)
    {
        builder.ToTable(
            TableNames.Tasks,
            SchemaNames.Data,
            table =>
            {
                table.HasCheckConstraint(
                    "ck_tasks_compensation_amount_non_negative",
                    "compensation_amount IS NULL OR compensation_amount >= 0");
                table.HasCheckConstraint(
                    "ck_tasks_distinct_profiles",
                    "accepted_helper_profile_id IS NULL OR seeker_profile_id <> accepted_helper_profile_id");
                table.HasCheckConstraint(
                    "ck_tasks_compensation_type",
                    ConfigurationHelpers.EnumValuesCheck<CompensationType>("compensation_type"));
                table.HasCheckConstraint(
                    "ck_tasks_status",
                    ConfigurationHelpers.EnumValuesCheck<DomainTaskStatus>("status"));
            });

        builder.HasGeneratedUuid(task => task.Id);
        builder.Property(task => task.PublicCode)
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValueSql("('TASK-' || to_char(now(), 'YYYY') || '-' || lpad(nextval('data.task_public_code_seq')::text, 6, '0'))")
            .ValueGeneratedOnAdd();
        builder.Property(task => task.Title).HasMaxLength(160).IsRequired();
        builder.Property(task => task.Description).HasMaxLength(3000).IsRequired();
        builder.Property(task => task.Location).HasColumnType("geography(Point,4326)");
        builder.Property(task => task.LocationText).HasMaxLength(300);
        builder.Property(task => task.CompensationType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(task => task.CompensationAmount).HasColumnType("numeric(12,2)");
        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(DomainTaskStatus.Open)
            .IsRequired();
        builder.HasCreatedAt(task => task.CreatedAt);
        builder.HasUpdatedAt(task => task.UpdatedAt);
        builder.Property(task => task.CancellationReason).HasMaxLength(1000);

        builder.HasOne(task => task.SeekerProfile)
            .WithMany(profile => profile.PostedTasks)
            .HasForeignKey(task => task.SeekerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.AcceptedHelperProfile)
            .WithMany(profile => profile.AcceptedTasks)
            .HasForeignKey(task => task.AcceptedHelperProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(task => task.Category)
            .WithMany(category => category.Tasks)
            .HasForeignKey(task => task.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(task => task.PublicCode)
            .IsUnique()
            .HasDatabaseName("ux_tasks_public_code");

        builder.HasIndex(task => new { task.Status, task.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_tasks_status_created_at");

        builder.HasIndex(task => new { task.CategoryId, task.Status })
            .HasDatabaseName("ix_tasks_category_status");

        builder.HasIndex(task => new { task.CompensationType, task.Status })
            .HasDatabaseName("ix_tasks_compensation_status");

        builder.HasIndex(task => task.SeekerProfileId)
            .HasDatabaseName("ix_tasks_seeker_profile_id");

        builder.HasIndex(task => task.AcceptedHelperProfileId)
            .HasDatabaseName("ix_tasks_accepted_helper_profile_id");

        builder.HasIndex(task => task.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_tasks_location");

        builder.HasIndex(task => task.CreatedAt)
            .IsDescending()
            .HasFilter("status = 'Open'")
            .HasDatabaseName("ix_tasks_open_created_at");

        builder.HasIndex(task => task.Title)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_tasks_title_trgm");

        builder.HasIndex(task => task.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("ix_tasks_description_trgm");
    }
}

internal sealed class TaskApplicationConfiguration : IEntityTypeConfiguration<TaskApplication>
{
    public void Configure(EntityTypeBuilder<TaskApplication> builder)
    {
        builder.ToTable(
            TableNames.TaskApplications,
            SchemaNames.Data,
            table => table.HasCheckConstraint(
                "ck_task_applications_status",
                ConfigurationHelpers.EnumValuesCheck<TaskApplicationStatus>("status")));

        builder.HasGeneratedUuid(application => application.Id);
        builder.Property(application => application.Message).HasMaxLength(1000);
        builder.Property(application => application.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(TaskApplicationStatus.Pending)
            .IsRequired();
        builder.HasCreatedAt(application => application.CreatedAt);
        builder.HasUpdatedAt(application => application.UpdatedAt);

        builder.HasOne(application => application.Task)
            .WithMany(task => task.Applications)
            .HasForeignKey(application => application.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.HelperProfile)
            .WithMany(profile => profile.TaskApplications)
            .HasForeignKey(application => application.HelperProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(application => new { application.TaskId, application.HelperProfileId })
            .IsUnique()
            .HasDatabaseName("ux_task_applications_task_helper");

        builder.HasIndex(application => application.TaskId)
            .HasDatabaseName("ix_task_applications_task_id");

        builder.HasIndex(application => application.HelperProfileId)
            .HasDatabaseName("ix_task_applications_helper_profile_id");

        builder.HasIndex(application => application.Status)
            .HasDatabaseName("ix_task_applications_status");
    }
}

internal sealed class TaskCompletionConfirmationConfiguration : IEntityTypeConfiguration<TaskCompletionConfirmation>
{
    public void Configure(EntityTypeBuilder<TaskCompletionConfirmation> builder)
    {
        builder.ToTable(TableNames.TaskCompletionConfirmations, SchemaNames.Data);

        builder.HasGeneratedUuid(confirmation => confirmation.Id);
        builder.HasCreatedAt(confirmation => confirmation.ConfirmedAt);

        builder.HasOne(confirmation => confirmation.Task)
            .WithMany(task => task.CompletionConfirmations)
            .HasForeignKey(confirmation => confirmation.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(confirmation => confirmation.Profile)
            .WithMany(profile => profile.CompletionConfirmations)
            .HasForeignKey(confirmation => confirmation.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(confirmation => new { confirmation.TaskId, confirmation.ProfileId })
            .IsUnique()
            .HasDatabaseName("ux_task_completion_confirmations_task_profile");

        builder.HasIndex(confirmation => confirmation.TaskId)
            .HasDatabaseName("ix_task_completion_confirmations_task_id");

        builder.HasIndex(confirmation => confirmation.ProfileId)
            .HasDatabaseName("ix_task_completion_confirmations_profile_id");
    }
}

internal sealed class TaskConversationConfiguration : IEntityTypeConfiguration<TaskConversation>
{
    public void Configure(EntityTypeBuilder<TaskConversation> builder)
    {
        builder.ToTable(TableNames.TaskConversations, SchemaNames.Data);

        builder.HasGeneratedUuid(conversation => conversation.Id);
        builder.Property(conversation => conversation.CosmosConversationId).HasMaxLength(200).IsRequired();
        builder.HasCreatedAt(conversation => conversation.CreatedAt);

        builder.HasOne(conversation => conversation.Task)
            .WithOne(task => task.Conversation)
            .HasForeignKey<TaskConversation>(conversation => conversation.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(conversation => conversation.SeekerProfile)
            .WithMany()
            .HasForeignKey(conversation => conversation.SeekerProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(conversation => conversation.HelperProfile)
            .WithMany()
            .HasForeignKey(conversation => conversation.HelperProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(conversation => conversation.TaskId)
            .IsUnique()
            .HasDatabaseName("ux_task_conversations_task_id");

        builder.HasIndex(conversation => conversation.CosmosConversationId)
            .IsUnique()
            .HasDatabaseName("ux_task_conversations_cosmos_conversation_id");

        builder.HasIndex(conversation => conversation.SeekerProfileId)
            .HasDatabaseName("ix_task_conversations_seeker_profile_id");

        builder.HasIndex(conversation => conversation.HelperProfileId)
            .HasDatabaseName("ix_task_conversations_helper_profile_id");
    }
}

internal sealed class TaskStatusHistoryEntryConfiguration : IEntityTypeConfiguration<TaskStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TaskStatusHistoryEntry> builder)
    {
        builder.ToTable(TableNames.TaskStatusHistory, SchemaNames.Log);

        builder.HasGeneratedUuid(history => history.Id);
        builder.Property(history => history.OldStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(history => history.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(history => history.Reason).HasMaxLength(1000);
        builder.HasCreatedAt(history => history.ChangedAt);

        builder.HasOne(history => history.Task)
            .WithMany(task => task.StatusHistory)
            .HasForeignKey(history => history.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.ChangedByProfile)
            .WithMany()
            .HasForeignKey(history => history.ChangedByProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.TaskId, history.ChangedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_task_status_history_task_id_changed_at");

        builder.HasIndex(history => history.ChangedByProfileId)
            .HasDatabaseName("ix_task_status_history_changed_by_profile_id");
    }
}
