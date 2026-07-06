namespace HowToSoftware.Migrator;

/// <summary>
/// Handles settings migration from Ghost MySQL dumps.
/// Processes the settings key-value table and collects migration statistics
/// including count by group and type.
/// </summary>
public static class SettingsMigrator
{
    /// <summary>
    /// Settings-related tables that this migrator tracks.
    /// </summary>
    private static readonly HashSet<string> SettingsTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "settings",
        "custom_theme_settings",
    };

    /// <summary>
    /// Ghost uses __GHOST_URL__ as a placeholder in some settings values.
    /// </summary>
    private const string GhostUrlPlaceholder = "__GHOST_URL__";

    /// <summary>
    /// Columns in the settings table whose values may contain __GHOST_URL__ references.
    /// </summary>
    private static readonly HashSet<string> UrlColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "value",
    };

    /// <summary>
    /// Processes parsed INSERT statements to collect settings migration statistics
    /// and optionally rewrite __GHOST_URL__ placeholders.
    /// </summary>
    /// <param name="inserts">Parsed INSERT statements from the MySQL dump.</param>
    /// <param name="siteUrl">Optional site URL to replace __GHOST_URL__ references.</param>
    /// <returns>Transformed inserts and settings migration statistics.</returns>
    public static SettingsMigrationResult ProcessSettings(
        IReadOnlyList<ParsedInsert> inserts, string? siteUrl = null)
    {
        var stats = new SettingsMigrationStats();
        var transformed = new List<ParsedInsert>(inserts.Count);

        foreach (var insert in inserts)
        {
            if (!SettingsTables.Contains(insert.TableName))
            {
                transformed.Add(insert);
                continue;
            }

            switch (insert.TableName.ToLowerInvariant())
            {
                case "settings":
                    var transformedSettings = ProcessSettingsTable(insert, stats, siteUrl);
                    transformed.Add(transformedSettings);
                    break;
                case "custom_theme_settings":
                    stats.CustomThemeSettingsCount += insert.Rows.Count;
                    transformed.Add(insert);
                    break;
            }
        }

        return new SettingsMigrationResult(transformed, stats);
    }

    /// <summary>
    /// Processes the settings table: collects group/type breakdown and rewrites URLs.
    /// </summary>
    private static ParsedInsert ProcessSettingsTable(
        ParsedInsert insert, SettingsMigrationStats stats, string? siteUrl)
    {
        var groupColIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("group", StringComparison.OrdinalIgnoreCase));
        var typeColIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("type", StringComparison.OrdinalIgnoreCase));
        var keyColIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("key", StringComparison.OrdinalIgnoreCase));
        var valueColIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("value", StringComparison.OrdinalIgnoreCase));

        var needsUrlRewrite = siteUrl is not null && valueColIdx >= 0;
        var newRows = needsUrlRewrite
            ? new List<string?[]>(insert.Rows.Count)
            : null;

        foreach (var row in insert.Rows)
        {
            stats.SettingsCount++;

            // Track group breakdown
            var group = groupColIdx >= 0 ? row[groupColIdx] : null;
            if (group is not null)
            {
                stats.CountByGroup.TryGetValue(group, out var gc);
                stats.CountByGroup[group] = gc + 1;
            }

            // Track type breakdown
            var type = typeColIdx >= 0 ? row[typeColIdx] : null;
            if (type is not null)
            {
                stats.CountByType.TryGetValue(type, out var tc);
                stats.CountByType[type] = tc + 1;
            }

            // Track key names
            var key = keyColIdx >= 0 ? row[keyColIdx] : null;
            if (key is not null)
                stats.Keys.Add(key);

            // Rewrite __GHOST_URL__ in value column
            if (needsUrlRewrite)
            {
                var newRow = new string?[row.Length];
                Array.Copy(row, newRow, row.Length);

                if (newRow[valueColIdx] is not null &&
                    newRow[valueColIdx]!.Contains(GhostUrlPlaceholder, StringComparison.Ordinal))
                {
                    newRow[valueColIdx] = newRow[valueColIdx]!
                        .Replace(GhostUrlPlaceholder, siteUrl!.TrimEnd('/'), StringComparison.Ordinal);
                    stats.UrlRewriteCount++;
                }

                newRows!.Add(newRow);
            }
        }

        if (newRows is not null)
            return new ParsedInsert(insert.TableName, insert.Columns, newRows);

        return insert;
    }

    /// <summary>
    /// Checks if a table name is a settings-related table.
    /// </summary>
    public static bool IsSettingsTable(string tableName)
        => SettingsTables.Contains(tableName);
}

/// <summary>
/// Result of settings migration processing.
/// </summary>
public sealed record SettingsMigrationResult(
    IReadOnlyList<ParsedInsert> TransformedInserts,
    SettingsMigrationStats Stats);

/// <summary>
/// Statistics collected during settings migration.
/// </summary>
public sealed class SettingsMigrationStats
{
    public int SettingsCount { get; set; }
    public int CustomThemeSettingsCount { get; set; }
    public int UrlRewriteCount { get; set; }

    /// <summary>
    /// Count of settings rows per group (e.g., "core", "members", "email").
    /// </summary>
    public Dictionary<string, int> CountByGroup { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Count of settings rows per type (e.g., "string", "boolean", "array").
    /// </summary>
    public Dictionary<string, int> CountByType { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All settings keys found during migration.
    /// </summary>
    public List<string> Keys { get; } = [];

    public int TotalCount => SettingsCount + CustomThemeSettingsCount;

    public override string ToString()
    {
        var groups = CountByGroup.Count > 0
            ? string.Join(", ", CountByGroup.Select(kv => $"{kv.Key}: {kv.Value}"))
            : "none";
        return $"Settings: {SettingsCount} ({groups}) | " +
               $"Theme settings: {CustomThemeSettingsCount} | " +
               $"URL rewrites: {UrlRewriteCount}";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
