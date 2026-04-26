using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(TableNames.Categories, SchemaNames.Config);

        builder.HasGeneratedUuid(category => category.Id);
        builder.Property(category => category.Code).HasMaxLength(64).IsRequired();
        builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.SortOrder).HasDefaultValue(0);
        builder.Property(category => category.IsActive).HasDefaultValue(true);
        builder.HasCreatedAt(category => category.CreatedAt);
        builder.HasUpdatedAt(category => category.UpdatedAt);

        builder.HasIndex(category => category.Code)
            .IsUnique()
            .HasDatabaseName("ux_categories_code");

        builder.HasIndex(category => category.IsActive)
            .HasDatabaseName("ix_categories_is_active");

        builder.HasIndex(category => category.SortOrder)
            .HasDatabaseName("ix_categories_sort_order");

        builder.HasData(
            SeedCategory("00000000-0000-0000-0000-000000000101", "moving", "Moving", 10),
            SeedCategory("00000000-0000-0000-0000-000000000102", "tutoring", "Tutoring", 20),
            SeedCategory("00000000-0000-0000-0000-000000000103", "repairs", "Repairs", 30),
            SeedCategory("00000000-0000-0000-0000-000000000104", "shopping", "Shopping", 40),
            SeedCategory("00000000-0000-0000-0000-000000000105", "pet_care", "Pet Care", 50),
            SeedCategory("00000000-0000-0000-0000-000000000106", "cleaning", "Cleaning", 60),
            SeedCategory("00000000-0000-0000-0000-000000000107", "errands", "Errands", 70),
            SeedCategory("00000000-0000-0000-0000-000000000108", "other", "Other", 80));
    }

    private static Category SeedCategory(string id, string code, string name, int sortOrder)
    {
        return new Category
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = ConfigurationHelpers.SeedTimestamp,
            UpdatedAt = ConfigurationHelpers.SeedTimestamp
        };
    }
}

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable(TableNames.Skills, SchemaNames.Config);

        builder.HasGeneratedUuid(skill => skill.Id);
        builder.Property(skill => skill.Code).HasMaxLength(64).IsRequired();
        builder.Property(skill => skill.Name).HasMaxLength(120).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(500);
        builder.Property(skill => skill.IsActive).HasDefaultValue(true);
        builder.HasCreatedAt(skill => skill.CreatedAt);
        builder.HasUpdatedAt(skill => skill.UpdatedAt);

        builder.HasIndex(skill => skill.Code)
            .IsUnique()
            .HasDatabaseName("ux_skills_code");

        builder.HasIndex(skill => skill.Name)
            .HasDatabaseName("ix_skills_name");

        builder.HasIndex(skill => skill.IsActive)
            .HasDatabaseName("ix_skills_is_active");

        builder.HasData(
            SeedSkill("00000000-0000-0000-0000-000000000201", "furniture_assembly", "Furniture Assembly"),
            SeedSkill("00000000-0000-0000-0000-000000000202", "moving_help", "Moving Help"),
            SeedSkill("00000000-0000-0000-0000-000000000203", "tutoring_math", "Math Tutoring"),
            SeedSkill("00000000-0000-0000-0000-000000000204", "tutoring_programming", "Programming Tutoring"),
            SeedSkill("00000000-0000-0000-0000-000000000205", "language_tutoring", "Language Tutoring"),
            SeedSkill("00000000-0000-0000-0000-000000000206", "pet_sitting", "Pet Sitting"),
            SeedSkill("00000000-0000-0000-0000-000000000207", "dog_walking", "Dog Walking"),
            SeedSkill("00000000-0000-0000-0000-000000000208", "shopping_help", "Shopping Help"),
            SeedSkill("00000000-0000-0000-0000-000000000209", "cleaning", "Cleaning"),
            SeedSkill("00000000-0000-0000-0000-000000000210", "minor_repairs", "Minor Repairs"),
            SeedSkill("00000000-0000-0000-0000-000000000211", "plumbing_basic", "Basic Plumbing"),
            SeedSkill("00000000-0000-0000-0000-000000000212", "electrical_basic", "Basic Electrical"),
            SeedSkill("00000000-0000-0000-0000-000000000213", "gardening", "Gardening"),
            SeedSkill("00000000-0000-0000-0000-000000000214", "cooking", "Cooking"),
            SeedSkill("00000000-0000-0000-0000-000000000215", "babysitting", "Babysitting"),
            SeedSkill("00000000-0000-0000-0000-000000000216", "elderly_help", "Elderly Help"),
            SeedSkill("00000000-0000-0000-0000-000000000217", "transport", "Transport"),
            SeedSkill("00000000-0000-0000-0000-000000000218", "computer_help", "Computer Help"),
            SeedSkill("00000000-0000-0000-0000-000000000219", "phone_setup", "Phone Setup"),
            SeedSkill("00000000-0000-0000-0000-000000000220", "other", "Other"));
    }

    private static Skill SeedSkill(string id, string code, string name)
    {
        return new Skill
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAt = ConfigurationHelpers.SeedTimestamp,
            UpdatedAt = ConfigurationHelpers.SeedTimestamp
        };
    }
}

internal sealed class TermsVersionConfiguration : IEntityTypeConfiguration<TermsVersion>
{
    public void Configure(EntityTypeBuilder<TermsVersion> builder)
    {
        builder.ToTable(TableNames.TermsVersions, SchemaNames.Config);

        builder.HasGeneratedUuid(terms => terms.Id);
        builder.Property(terms => terms.Version).HasMaxLength(32).IsRequired();
        builder.Property(terms => terms.Title).HasMaxLength(200).IsRequired();
        builder.Property(terms => terms.ContentUrl);
        builder.Property(terms => terms.IsActive).HasDefaultValue(true);
        builder.Property(terms => terms.EffectiveFrom).IsRequired();
        builder.HasCreatedAt(terms => terms.CreatedAt);
        builder.HasUpdatedAt(terms => terms.UpdatedAt);

        builder.HasIndex(terms => terms.Version)
            .IsUnique()
            .HasDatabaseName("ux_terms_versions_version");

        builder.HasIndex(terms => terms.IsActive)
            .HasDatabaseName("ix_terms_versions_is_active");

        builder.HasIndex(terms => terms.EffectiveFrom)
            .HasDatabaseName("ix_terms_versions_effective_from");

        builder.HasData(new TermsVersion
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000301"),
            Version = "1.0",
            Title = "Initial Terms and Conditions",
            IsActive = true,
            EffectiveFrom = ConfigurationHelpers.SeedTimestamp,
            CreatedAt = ConfigurationHelpers.SeedTimestamp,
            UpdatedAt = ConfigurationHelpers.SeedTimestamp
        });
    }
}
