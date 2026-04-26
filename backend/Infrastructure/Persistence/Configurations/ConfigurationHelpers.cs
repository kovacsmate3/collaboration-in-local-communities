using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

internal static class ConfigurationHelpers
{
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static PropertyBuilder<Guid> HasGeneratedUuid<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Guid>> propertyExpression)
        where TEntity : class
    {
        return builder.Property(propertyExpression)
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();
    }

    public static PropertyBuilder<DateTimeOffset> HasCreatedAt<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, DateTimeOffset>> propertyExpression)
        where TEntity : class
    {
        return builder.Property(propertyExpression)
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }

    public static PropertyBuilder<DateTimeOffset> HasUpdatedAt<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, DateTimeOffset>> propertyExpression)
        where TEntity : class
    {
        return builder.Property(propertyExpression)
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }

    public static string EnumValuesCheck<TEnum>(string columnName)
        where TEnum : struct, Enum
    {
        var values = Enum.GetNames<TEnum>()
            .Select(value => $"'{value}'");

        return $"{columnName} IN ({string.Join(", ", values)})";
    }

    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var viewName = entityType.GetViewName();
            if (!string.IsNullOrWhiteSpace(viewName))
            {
                entityType.SetViewName(ToSnakeCase(viewName));
                entityType.SetTableName(null);
            }

            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(viewName) && !string.IsNullOrWhiteSpace(tableName))
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.View)
                ?? StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);

            foreach (var property in entityType.GetProperties())
            {
                var columnName = storeObject.HasValue
                    ? property.GetColumnName(storeObject.Value)
                    : property.Name;

                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }

            foreach (var key in entityType.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var constraintName = foreignKey.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(constraintName))
                {
                    foreignKey.SetConstraintName(ToSnakeCase(constraintName));
                }
            }

            foreach (var index in entityType.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current is '-' or ' ')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(current))
            {
                if (i > 0 && value[i - 1] != '_' && !char.IsUpper(value[i - 1]))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}
