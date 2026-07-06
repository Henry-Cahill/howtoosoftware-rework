using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace HowToSoftware.Infrastructure.Data;

public static partial class SnakeCaseExtensions
{
    public static ModelBuilder ApplySnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null)
                entity.SetTableName(ToSnakeCase(tableName));

            var schema = entity.GetSchema();
            if (schema is not null)
                entity.SetSchema(ToSnakeCase(schema));

            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.GetColumnName()!));

            foreach (var key in entity.GetKeys())
            {
                var name = key.GetName();
                if (name is not null)
                    key.SetName(ToSnakeCase(name));
            }

            foreach (var index in entity.GetIndexes())
            {
                var name = index.GetDatabaseName();
                if (name is not null)
                    index.SetDatabaseName(ToSnakeCase(name));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                var name = fk.GetConstraintName();
                if (name is not null)
                    fk.SetConstraintName(ToSnakeCase(name));
            }
        }

        return modelBuilder;
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = BeforeUpperAfterLower().Replace(input, "$1_$2");
        result = UpperRunBeforeUpperLower().Replace(result, "$1_$2");
        return result.ToLowerInvariant();
    }

    [GeneratedRegex(@"([a-z0-9])([A-Z])")]
    private static partial Regex BeforeUpperAfterLower();

    [GeneratedRegex(@"([A-Z]+)([A-Z][a-z])")]
    private static partial Regex UpperRunBeforeUpperLower();
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
