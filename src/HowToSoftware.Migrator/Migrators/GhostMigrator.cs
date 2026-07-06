namespace HowToSoftware.Migrator;

/// <summary>
/// Orchestrates the Ghost MySQL → SQL Server migration.
/// Reads one or more MySQL dump files, parses the INSERT statements,
/// applies content transforms (URL rewriting), and generates a T-SQL migration script.
/// </summary>
public sealed class GhostMigrator
{
    /// <summary>
    /// Runs the migration: parses MySQL dump file(s) and writes a T-SQL script to the output path.
    /// </summary>
    /// <param name="dumpFilePaths">Paths to MySQL dump (.sql) files to process.</param>
    /// <param name="outputPath">Path where the generated T-SQL script will be written.</param>
    /// <param name="siteUrl">Optional new site URL to replace __GHOST_URL__ references. If null, no URL rewriting is performed.</param>
    /// <returns>Migration statistics.</returns>
    public MigrationResult Migrate(IReadOnlyList<string> dumpFilePaths, string outputPath, string? siteUrl = null)
    {
        var allInserts = new List<ParsedInsert>();
        var totalRowCount = 0;
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dumpFile in dumpFilePaths)
        {
            if (!File.Exists(dumpFile))
                throw new FileNotFoundException($"MySQL dump file not found: {dumpFile}");

            Console.WriteLine($"Parsing: {dumpFile}");

            foreach (var insert in MySqlDumpParser.Parse(dumpFile))
            {
                allInserts.Add(insert);
                totalRowCount += insert.Rows.Count;
                tableNames.Add(insert.TableName);
                Console.WriteLine($"  Found {insert.Rows.Count} rows in `{insert.TableName}`");
            }
        }

        if (allInserts.Count == 0)
        {
            Console.WriteLine("No INSERT statements found in the provided dump files.");
            return new MigrationResult(0, 0, [], null, null, null, null, null);
        }

        // Apply content migration transforms (URL rewriting + stats collection)
        ContentMigrationStats? contentStats = null;
        if (siteUrl is not null)
        {
            Console.WriteLine($"\nApplying content migration transforms (site URL: {siteUrl})...");
            var contentResult = PostContentMigrator.ProcessContent(allInserts, siteUrl);
            allInserts = [.. contentResult.TransformedInserts];
            contentStats = contentResult.Stats;
            Console.WriteLine($"  Content stats: {contentStats}");
        }

        // Collect member migration statistics
        MemberMigrationStats? memberStats = null;
        var memberInserts = allInserts.Where(i => MemberMigrator.IsMemberTable(i.TableName)).ToList();
        if (memberInserts.Count > 0)
        {
            Console.WriteLine("\nProcessing member migration...");
            var memberResult = MemberMigrator.ProcessMembers(allInserts);
            memberStats = memberResult.Stats;
            Console.WriteLine($"  Member stats: {memberStats}");
        }

        // Process settings migration (URL rewriting + stats collection)
        SettingsMigrationStats? settingsStats = null;
        var settingsInserts = allInserts.Where(i => SettingsMigrator.IsSettingsTable(i.TableName)).ToList();
        if (settingsInserts.Count > 0)
        {
            Console.WriteLine("\nProcessing settings migration...");
            var settingsResult = SettingsMigrator.ProcessSettings(allInserts, siteUrl);
            allInserts = [.. settingsResult.TransformedInserts];
            settingsStats = settingsResult.Stats;
            Console.WriteLine($"  Settings stats: {settingsStats}");
        }

        // Process analytics migration (table rename + column mapping)
        AnalyticsMigrationStats? analyticsStats = null;
        var analyticsInserts = allInserts.Where(i => AnalyticsMigrator.IsAnalyticsTable(i.TableName)).ToList();
        if (analyticsInserts.Count > 0)
        {
            Console.WriteLine("\nProcessing analytics migration...");
            var analyticsResult = AnalyticsMigrator.ProcessAnalytics(allInserts);
            allInserts = [.. analyticsResult.TransformedInserts];
            analyticsStats = analyticsResult.Stats;
            Console.WriteLine($"  Analytics stats: {analyticsStats}");
        }

        // Rewrite absolute image URLs to relative paths
        ImageUrlRewriteStats? imageStats = null;
        if (siteUrl is not null)
        {
            Console.WriteLine("\nRewriting image URLs to relative paths...");
            var imageResult = ImageMigrator.RewriteImageUrls(allInserts, siteUrl);
            allInserts = [.. imageResult.TransformedInserts];
            imageStats = imageResult.Stats;
            Console.WriteLine($"  Image stats: {imageStats}");

            // Report referenced image paths
            var imagePaths = ImageMigrator.FindImageReferences(allInserts);
            if (imagePaths.Count > 0)
            {
                Console.WriteLine($"  Found {imagePaths.Count} unique image path(s) in content");
            }
        }

        Console.WriteLine($"\nGenerating T-SQL for {totalRowCount} total rows across {tableNames.Count} tables...");

        var tsql = TSqlGenerator.Generate(allInserts);

        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        File.WriteAllText(outputPath, tsql);

        Console.WriteLine($"T-SQL script written to: {outputPath}");
        Console.WriteLine($"  Tables: {tableNames.Count}");
        Console.WriteLine($"  Total rows: {totalRowCount}");

        return new MigrationResult(tableNames.Count, totalRowCount, [.. tableNames], contentStats, memberStats, settingsStats, imageStats, analyticsStats);
    }
}

/// <summary>
/// Summary statistics from a migration run.
/// </summary>
public sealed record MigrationResult(
    int TableCount,
    int RowCount,
    string[] TableNames,
    ContentMigrationStats? ContentStats,
    MemberMigrationStats? MemberStats,
    SettingsMigrationStats? SettingsStats,
    ImageUrlRewriteStats? ImageStats,
    AnalyticsMigrationStats? AnalyticsStats);

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
