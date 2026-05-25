using Backend.Domain.Entities;
using Backend.Domain.Enums;
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
        builder.Property(category => category.Icon)
            .HasMaxLength(64)
            .HasDefaultValue(Category.DefaultIcon)
            .IsRequired();
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
            SeedCategory("00000000-0000-0000-0000-000000000101", "moving", "Moving", "DeliveryTruck01Icon", 10),
            SeedCategory("00000000-0000-0000-0000-000000000102", "tutoring", "Tutoring", "Mortarboard02Icon", 20),
            SeedCategory("00000000-0000-0000-0000-000000000103", "repairs", "Repairs", "Wrench01Icon", 30),
            SeedCategory("00000000-0000-0000-0000-000000000104", "shopping", "Shopping", "ShoppingBag03Icon", 40),
            SeedCategory("00000000-0000-0000-0000-000000000105", "pet_care", "Pet Care", "Bone01Icon", 50),
            SeedCategory("00000000-0000-0000-0000-000000000106", "cleaning", "Cleaning", "SparklesIcon", 60),
            SeedCategory("00000000-0000-0000-0000-000000000107", "errands", "Errands", "RunningShoesIcon", 70),
            SeedCategory("00000000-0000-0000-0000-000000000108", "other", "Other", Category.DefaultIcon, 80));
    }

    private static Category SeedCategory(string id, string code, string name, string icon, int sortOrder)
    {
        return new Category
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Icon = icon,
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
        builder.ToTable(
            TableNames.Skills,
            SchemaNames.Config,
            table => table.HasCheckConstraint(
                "ck_skills_status",
                ConfigurationHelpers.EnumValuesCheck<SkillStatus>("status")));

        builder.HasGeneratedUuid(skill => skill.Id);
        builder.Property(skill => skill.Code).HasMaxLength(64).IsRequired();
        builder.Property(skill => skill.Name).HasMaxLength(120).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(500);
        builder.Property(skill => skill.IsActive).HasDefaultValue(true);
        builder.Property(skill => skill.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(SkillStatus.Pending)
            .IsRequired();
        builder.Property(skill => skill.ApprovedAt);
        builder.HasCreatedAt(skill => skill.CreatedAt);
        builder.HasUpdatedAt(skill => skill.UpdatedAt);

        builder.HasIndex(skill => skill.Code)
            .IsUnique()
            .HasDatabaseName("ux_skills_code");

        builder.HasIndex(skill => skill.Name)
            .HasDatabaseName("ix_skills_name");

        builder.HasIndex(skill => skill.IsActive)
            .HasDatabaseName("ix_skills_is_active");

        builder.HasIndex(skill => skill.Status)
            .HasDatabaseName("ix_skills_status");

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
            Status = SkillStatus.Approved,
            ApprovedAt = ConfigurationHelpers.SeedTimestamp,
            CreatedAt = ConfigurationHelpers.SeedTimestamp,
            UpdatedAt = ConfigurationHelpers.SeedTimestamp
        };
    }
}

internal sealed class TermsVersionConfiguration : IEntityTypeConfiguration<TermsVersion>
{
    private const string PlaceholderContent =
        "<h2>1. About 2gather</h2>" +
        "<p>2gather is a platform that connects neighbours who need help with those willing to offer it. " +
        "By creating an account you agree to use the service in good faith and in accordance with your local laws.</p>" +
        "<h2>2. Your account</h2>" +
        "<p>You are responsible for keeping your credentials secure and for all activity that occurs under your account. " +
        "Notify us immediately if you suspect unauthorised access.</p>" +
        "<h2>3. Acceptable use</h2>" +
        "<p>You may not use 2gather to post illegal tasks, harass other users, misrepresent your identity, " +
        "or scrape the platform without written permission. We reserve the right to suspend accounts that violate these rules.</p>" +
        "<h2>4. Payments &amp; barter</h2>" +
        "<p>2gather facilitates agreements between users but is not a party to them. " +
        "Any payment or exchange is solely between the Seeker and the Helper. We are not liable for disputes arising from tasks.</p>" +
        "<h2>5. Privacy</h2>" +
        "<p>We collect only the information needed to operate the service. We do not sell your data. " +
        "A full privacy policy will be published before the public launch.</p>" +
        "<h2>6. Limitation of liability</h2>" +
        "<p>2gather is provided “as is” during this early phase. " +
        "We make no warranties about uptime or fitness for a particular purpose. " +
        "Our liability is limited to the maximum extent permitted by applicable law.</p>" +
        "<h2>7. Changes to these terms</h2>" +
        "<p>We may update these terms as the product evolves. " +
        "Continued use of the platform after changes are posted constitutes acceptance of the revised terms.</p>";

    public void Configure(EntityTypeBuilder<TermsVersion> builder)
    {
        builder.ToTable(TableNames.TermsVersions, SchemaNames.Config);

        builder.HasGeneratedUuid(terms => terms.Id);
        builder.Property(terms => terms.Version).HasMaxLength(32).IsRequired();
        builder.Property(terms => terms.MajorVersion).IsRequired();
        builder.Property(terms => terms.MinorVersion).IsRequired();
        builder.Property(terms => terms.PatchVersion).IsRequired();
        builder.Property(terms => terms.Title).HasMaxLength(200).IsRequired();
        builder.Property(terms => terms.Content);
        builder.Property(terms => terms.ContentUrl).HasMaxLength(2048);
        builder.Property(terms => terms.IsActive).HasDefaultValue(false);
        builder.Property(terms => terms.EffectiveFrom).IsRequired();
        builder.HasCreatedAt(terms => terms.CreatedAt);
        builder.HasUpdatedAt(terms => terms.UpdatedAt);

        builder.HasIndex(t => new { t.MajorVersion, t.MinorVersion, t.PatchVersion })
            .IsUnique()
            .HasDatabaseName("ux_terms_versions_version_triple");

        builder.HasIndex(t => new { t.MajorVersion, t.MinorVersion })
            .HasDatabaseName("ix_terms_versions_major_minor");

        builder.HasIndex(terms => terms.IsActive)
            .HasDatabaseName("ix_terms_versions_is_active");

        builder.HasIndex(terms => terms.EffectiveFrom)
            .HasDatabaseName("ix_terms_versions_effective_from");

        builder.HasData(new TermsVersion
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000301"),
            Version = "0.1.0",
            MajorVersion = 0,
            MinorVersion = 1,
            PatchVersion = 0,
            Title = "Initial Terms and Conditions",
            Content = PlaceholderContent,
            IsActive = true,
            EffectiveFrom = ConfigurationHelpers.SeedTimestamp,
            CreatedAt = ConfigurationHelpers.SeedTimestamp,
            UpdatedAt = ConfigurationHelpers.SeedTimestamp
        });
    }
}
