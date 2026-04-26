using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

internal sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable(TableNames.ActivityEvents, SchemaNames.Log);

        builder.HasGeneratedUuid(activityEvent => activityEvent.Id);
        builder.Property(activityEvent => activityEvent.EventType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(activityEvent => activityEvent.EntityType).HasMaxLength(64);
        builder.Property(activityEvent => activityEvent.Metadata).HasColumnType("jsonb");
        builder.HasCreatedAt(activityEvent => activityEvent.CreatedAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(activityEvent => activityEvent.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(activityEvent => activityEvent.Profile)
            .WithMany()
            .HasForeignKey(activityEvent => activityEvent.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(activityEvent => new { activityEvent.UserId, activityEvent.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_activity_events_user_id_created_at");

        builder.HasIndex(activityEvent => new { activityEvent.ProfileId, activityEvent.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_activity_events_profile_id_created_at");

        builder.HasIndex(activityEvent => new { activityEvent.EventType, activityEvent.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_activity_events_event_type_created_at");

        builder.HasIndex(activityEvent => activityEvent.CreatedAt)
            .HasDatabaseName("ix_activity_events_created_at");
    }
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable(TableNames.AuditEvents, SchemaNames.Log);

        builder.HasGeneratedUuid(auditEvent => auditEvent.Id);
        builder.Property(auditEvent => auditEvent.EventType).HasMaxLength(100).IsRequired();
        builder.Property(auditEvent => auditEvent.EntityType).HasMaxLength(100);
        builder.Property(auditEvent => auditEvent.Payload).HasColumnType("jsonb");
        builder.HasCreatedAt(auditEvent => auditEvent.CreatedAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(auditEvent => new { auditEvent.ActorUserId, auditEvent.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_events_actor_user_id_created_at");

        builder.HasIndex(auditEvent => new { auditEvent.EntityType, auditEvent.EntityId })
            .HasDatabaseName("ix_audit_events_entity");

        builder.HasIndex(auditEvent => auditEvent.CreatedAt)
            .HasDatabaseName("ix_audit_events_created_at");
    }
}
