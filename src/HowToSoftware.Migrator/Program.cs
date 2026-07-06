using HowToSoftware.Migrator;

if (args.Length == 0)
{
    Console.WriteLine("Ghost MySQL → SQL Server Migrator");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  HowToSoftware.Migrator <dump1.sql> [dump2.sql ...] [-o output.sql]");
    Console.WriteLine("  HowToSoftware.Migrator <dump1.sql> --execute -c <connection-string>");
    Console.WriteLine("  HowToSoftware.Migrator <dump1.sql> --verify [--site-url <url>]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <dump.sql>       One or more MySQL dump files to process");
    Console.WriteLine("  -o <path>        Output path for generated T-SQL script (default: migration_output.sql)");
    Console.WriteLine("  --site-url <url> New site URL to replace __GHOST_URL__ references");
    Console.WriteLine("  --images-src <path>  Source Ghost content/images directory to copy");
    Console.WriteLine("  --images-dst <path>  Destination directory for copied images (e.g., wwwroot/content/images)");
    Console.WriteLine("  --execute        Execute the generated T-SQL directly against SQL Server");
    Console.WriteLine("  -c <conn>        SQL Server connection string (required with --execute)");
    Console.WriteLine("  --verify         Verify migration: compare Ghost HTML vs clone-rendered HTML for every post/page");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  HowToSoftware.Migrator DOCS/ghost_posts_dump.sql -o migration.sql");
    Console.WriteLine("  HowToSoftware.Migrator DOCS/ghost_posts_dump.sql --site-url https://howtoosoftware.com -o migration.sql");
    Console.WriteLine("  HowToSoftware.Migrator DOCS/ghost_posts_dump.sql --execute -c \"Server=...\"");
    return 1;
}

// Parse CLI arguments
var dumpFiles = new List<string>();
var outputPath = "migration_output.sql";
var execute = false;
var verify = false;
string? connectionString = null;
string? siteUrl = null;
string? imagesSrc = null;
string? imagesDst = null;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "-o" && i + 1 < args.Length)
    {
        outputPath = args[++i];
    }
    else if (args[i] == "--site-url" && i + 1 < args.Length)
    {
        siteUrl = args[++i];
    }
    else if (args[i] == "--images-src" && i + 1 < args.Length)
    {
        imagesSrc = args[++i];
    }
    else if (args[i] == "--images-dst" && i + 1 < args.Length)
    {
        imagesDst = args[++i];
    }
    else if (args[i] == "--execute")
    {
        execute = true;
    }
    else if (args[i] == "--verify")
    {
        verify = true;
    }
    else if (args[i] == "-c" && i + 1 < args.Length)
    {
        connectionString = args[++i];
    }
    else
    {
        dumpFiles.Add(args[i]);
    }
}

if (dumpFiles.Count == 0)
{
    Console.Error.WriteLine("Error: No dump files specified.");
    return 1;
}

if (execute && string.IsNullOrEmpty(connectionString))
{
    Console.Error.WriteLine("Error: --execute requires -c <connection-string>.");
    return 1;
}

try
{
    // Verification mode: compare Ghost HTML vs clone-rendered HTML
    if (verify)
    {
        Console.WriteLine("\nRunning migration verification...");
        var allInserts = new List<ParsedInsert>();
        foreach (var dumpFile in dumpFiles)
        {
            if (!File.Exists(dumpFile))
            {
                Console.Error.WriteLine($"Error: File not found: {dumpFile}");
                return 1;
            }
            foreach (var insert in MySqlDumpParser.Parse(dumpFile))
                allInserts.Add(insert);
        }

        var verifier = new MigrationVerifier();
        var report = verifier.Verify(allInserts, siteUrl);
        Console.WriteLine();
        Console.WriteLine(report);
        return report.MismatchCount + report.RenderErrorCount > 0 ? 1 : 0;
    }

    // Copy images if source/destination specified
    if (imagesSrc is not null && imagesDst is not null)
    {
        Console.WriteLine($"\nCopying images from {imagesSrc} to {imagesDst}...");
        var copyStats = ImageMigrator.CopyImages(imagesSrc, imagesDst);
        Console.WriteLine($"  {copyStats}");
    }
    else if (imagesSrc is not null || imagesDst is not null)
    {
        Console.Error.WriteLine("Error: --images-src and --images-dst must both be specified.");
        return 1;
    }

    var migrator = new GhostMigrator();
    var result = migrator.Migrate(dumpFiles, outputPath, siteUrl);
    Console.WriteLine($"\nMigration complete: {result.TableCount} tables, {result.RowCount} rows.");

    if (execute)
    {
        Console.WriteLine($"\nExecuting T-SQL against SQL Server...");
        await SqlExecutor.ExecuteScriptAsync(outputPath, connectionString!);
        Console.WriteLine("Database migration executed successfully.");
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
