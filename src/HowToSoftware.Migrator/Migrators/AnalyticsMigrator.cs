namespace HowToSoftware.Migrator;

/// <summary>
/// Handles analytics data migration from the Ghost MySQL <c>tinybird_analytics_backup</c> table
/// to the SQL Server <c>analytics_events</c> table.
/// Renames the table and columns to match the target schema, and collects migration statistics.
/// </summary>
public static class AnalyticsMigrator
{
    /// <summary>
    /// The source table name in the Ghost MySQL dump.
    /// </summary>
    private const string SourceTableName = "tinybird_analytics_backup";

    /// <summary>
    /// The target table name in the SQL Server schema.
    /// </summary>
    private const string TargetTableName = "analytics_events";

    /// <summary>
    /// Column name mappings from Ghost MySQL → SQL Server (only those that differ).
    /// </summary>
    private static readonly Dictionary<string, string> ColumnRenames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["page_urlpath"] = "page_url_path",
    };

    /// <summary>
    /// Checks if a table name is the analytics backup table.
    /// </summary>
    public static bool IsAnalyticsTable(string tableName)
        => tableName.Equals(SourceTableName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Processes parsed INSERT statements to transform analytics data and collect statistics.
    /// Renames the table from <c>tinybird_analytics_backup</c> to <c>analytics_events</c>,
    /// remaps column names, and strips the auto-increment <c>id</c> column so SQL Server
    /// generates identity values.
    /// </summary>
    /// <param name="inserts">All parsed INSERT statements from the MySQL dump.</param>
    /// <returns>Transformed inserts (with renamed table/columns) and migration statistics.</returns>
    public static AnalyticsMigrationResult ProcessAnalytics(IReadOnlyList<ParsedInsert> inserts)
    {
        var stats = new AnalyticsMigrationStats();
        var transformed = new List<ParsedInsert>(inserts.Count);

        foreach (var insert in inserts)
        {
            if (!IsAnalyticsTable(insert.TableName))
            {
                transformed.Add(insert);
                continue;
            }

            // Rename columns (page_urlpath → page_url_path) and drop the id column
            var idColIdx = Array.FindIndex(insert.Columns,
                c => c.Equals("id", StringComparison.OrdinalIgnoreCase));

            var newColumns = insert.Columns
                .Where((_, i) => i != idColIdx)
                .Select(c => ColumnRenames.TryGetValue(c, out var mapped) ? mapped : c)
                .ToArray();

            // Rebuild rows without the id column
            var newRows = new List<string?[]>(insert.Rows.Count);
            foreach (var row in insert.Rows)
            {
                var newRow = row
                    .Where((_, i) => i != idColIdx)
                    .ToArray();
                newRows.Add(newRow);
            }

            transformed.Add(new ParsedInsert(TargetTableName, newColumns, newRows));

            // Collect statistics
            CollectStats(insert, stats);
        }

        return new AnalyticsMigrationResult(transformed, stats);
    }

    private static void CollectStats(ParsedInsert insert, AnalyticsMigrationStats stats)
    {
        var actionIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("action", StringComparison.OrdinalIgnoreCase));
        var deviceIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("device", StringComparison.OrdinalIgnoreCase));
        var browserIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("browser", StringComparison.OrdinalIgnoreCase));
        var osIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("os", StringComparison.OrdinalIgnoreCase));
        var countryIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("country", StringComparison.OrdinalIgnoreCase));
        var memberStatusIdx = Array.FindIndex(insert.Columns,
            c => c.Equals("member_status", StringComparison.OrdinalIgnoreCase));

        foreach (var row in insert.Rows)
        {
            stats.EventCount++;

            IncrementBucket(stats.ActionCounts, row, actionIdx);
            IncrementBucket(stats.DeviceCounts, row, deviceIdx);
            IncrementBucket(stats.BrowserCounts, row, browserIdx);
            IncrementBucket(stats.OsCounts, row, osIdx);
            IncrementBucket(stats.CountryCounts, row, countryIdx);
            IncrementBucket(stats.MemberStatusCounts, row, memberStatusIdx);
        }
    }

    private static void IncrementBucket(Dictionary<string, int> buckets, string?[] row, int colIdx)
    {
        if (colIdx < 0 || colIdx >= row.Length)
            return;
        var value = row[colIdx] ?? "(null)";
        buckets.TryGetValue(value, out var count);
        buckets[value] = count + 1;
    }
}

/// <summary>
/// Result of analytics migration processing.
/// </summary>
public sealed record AnalyticsMigrationResult(
    IReadOnlyList<ParsedInsert> TransformedInserts,
    AnalyticsMigrationStats Stats);

/// <summary>
/// Statistics collected during analytics migration.
/// </summary>
public sealed class AnalyticsMigrationStats
{
    public int EventCount { get; set; }
    public Dictionary<string, int> ActionCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> DeviceCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> BrowserCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> OsCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> CountryCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> MemberStatusCounts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override string ToString()
    {
        var actions = string.Join(", ", ActionCounts.Select(kv => $"{kv.Key}: {kv.Value}"));
        return $"Events: {EventCount} | Actions: [{actions}]";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
