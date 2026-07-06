using System.Text.RegularExpressions;

namespace HowToSoftware.Migrator;

/// <summary>
/// Handles image migration from a Ghost content/images directory.
/// Copies the image directory tree to the target location and rewrites
/// absolute image URLs in post content to relative paths.
/// </summary>
public static partial class ImageMigrator
{
    /// <summary>
    /// Content columns in the posts table (and related tables) that may contain image URL references.
    /// </summary>
    private static readonly HashSet<string> ContentColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "mobiledoc", "lexical", "html", "plaintext",
        "feature_image", "canonical_url",
        "codeinjection_head", "codeinjection_foot",
        "value", // settings table
        "og_image", "twitter_image", // posts_meta
    };

    /// <summary>
    /// Tables whose rows may contain image URL references worth rewriting.
    /// </summary>
    private static readonly HashSet<string> ImageReferenceTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "posts", "posts_meta", "post_revisions", "mobiledoc_revisions",
        "settings", "custom_theme_settings",
        "users",
    };

    /// <summary>
    /// Copies all files from a Ghost content/images source directory into a target directory,
    /// preserving the subdirectory structure (e.g., 2026/03/photo.png).
    /// </summary>
    /// <param name="sourceDir">Ghost's content/images directory path.</param>
    /// <param name="targetDir">Destination directory (e.g., wwwroot/content/images).</param>
    /// <returns>Statistics about the copy operation.</returns>
    public static ImageCopyStats CopyImages(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source images directory not found: {sourceDir}");

        var stats = new ImageCopyStats();

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var destFile = Path.Combine(targetDir, relativePath);

            var destDir = Path.GetDirectoryName(destFile)!;
            Directory.CreateDirectory(destDir);

            File.Copy(sourceFile, destFile, overwrite: true);
            stats.FilesCopied++;
            stats.TotalBytes += new FileInfo(sourceFile).Length;

            var ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            stats.CountByExtension.TryGetValue(ext, out var count);
            stats.CountByExtension[ext] = count + 1;
        }

        // Count subdirectories
        stats.DirectoriesCreated = Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories).Count();

        return stats;
    }

    /// <summary>
    /// Rewrites absolute image URLs in parsed INSERT data to relative paths.
    /// Converts patterns like "https://howtoosoftware.com/content/images/..." to "/content/images/...".
    /// </summary>
    /// <param name="inserts">Parsed INSERT statements from the MySQL dump.</param>
    /// <param name="siteUrl">The site URL whose absolute image references should be made relative.</param>
    /// <returns>Transformed inserts and image migration statistics.</returns>
    public static ImageMigrationResult RewriteImageUrls(
        IReadOnlyList<ParsedInsert> inserts, string siteUrl)
    {
        var normalizedUrl = siteUrl.TrimEnd('/');
        var stats = new ImageUrlRewriteStats();
        var transformed = new List<ParsedInsert>(inserts.Count);

        foreach (var insert in inserts)
        {
            if (!ImageReferenceTables.Contains(insert.TableName))
            {
                transformed.Add(insert);
                continue;
            }

            // Check if any column is worth scanning
            var hasContentCols = insert.Columns.Any(c => ContentColumns.Contains(c));
            if (!hasContentCols)
            {
                transformed.Add(insert);
                continue;
            }

            var newRows = new List<string?[]>(insert.Rows.Count);
            foreach (var row in insert.Rows)
            {
                var newRow = new string?[row.Length];
                Array.Copy(row, newRow, row.Length);

                for (var i = 0; i < insert.Columns.Length && i < newRow.Length; i++)
                {
                    if (newRow[i] is null || !ContentColumns.Contains(insert.Columns[i]))
                        continue;

                    var original = newRow[i]!;
                    var rewritten = RewriteAbsoluteImageUrls(original, normalizedUrl);

                    if (!ReferenceEquals(original, rewritten))
                    {
                        newRow[i] = rewritten;
                        stats.UrlsRewritten++;
                    }
                }

                newRows.Add(newRow);
            }

            stats.RowsScanned += insert.Rows.Count;
            transformed.Add(new ParsedInsert(insert.TableName, insert.Columns, newRows));
        }

        return new ImageMigrationResult(transformed, stats);
    }

    /// <summary>
    /// Rewrites absolute image URLs to relative paths within a content string.
    /// Example: "https://howtoosoftware.com/content/images/2026/03/photo.png"
    ///       → "/content/images/2026/03/photo.png"
    /// </summary>
    internal static string RewriteAbsoluteImageUrls(string content, string siteUrl)
    {
        // Pattern: siteUrl + /content/images/...
        // This handles both unescaped URLs and JSON-escaped URLs (with \/ or \\/)
        var prefix = siteUrl + "/content/images/";

        if (!content.Contains("/content/images/", StringComparison.Ordinal) &&
            !content.Contains("\\/content\\/images\\/", StringComparison.Ordinal))
            return content;

        // Replace absolute site URL + /content/images/ with relative /content/images/
        if (content.Contains(prefix, StringComparison.OrdinalIgnoreCase))
        {
            content = content.Replace(prefix, "/content/images/", StringComparison.OrdinalIgnoreCase);
        }

        // Handle JSON-escaped variants: siteUrl\/content\/images\/
        var escapedPrefix = siteUrl.Replace("/", "\\/") + "\\/content\\/images\\/";
        if (content.Contains(escapedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            content = content.Replace(escapedPrefix, "\\/content\\/images\\/", StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }

    /// <summary>
    /// Scans content columns for image references and returns all unique image paths found.
    /// Useful for verifying all referenced images exist on disk.
    /// </summary>
    public static HashSet<string> FindImageReferences(IReadOnlyList<ParsedInsert> inserts)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var insert in inserts)
        {
            if (!ImageReferenceTables.Contains(insert.TableName))
                continue;

            for (var i = 0; i < insert.Columns.Length; i++)
            {
                if (!ContentColumns.Contains(insert.Columns[i]))
                    continue;

                foreach (var row in insert.Rows)
                {
                    if (row.Length <= i || row[i] is null)
                        continue;

                    foreach (var match in ImagePathPattern().Matches(row[i]!).Cast<Match>())
                    {
                        paths.Add(match.Value);
                    }
                }
            }
        }

        return paths;
    }

    [GeneratedRegex(@"/content/images/[^\s""'\\,)}\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex ImagePathPattern();
}

/// <summary>
/// Result of image URL rewriting in migration data.
/// </summary>
public sealed record ImageMigrationResult(
    IReadOnlyList<ParsedInsert> TransformedInserts,
    ImageUrlRewriteStats Stats);

/// <summary>
/// Statistics from copying image files.
/// </summary>
public sealed class ImageCopyStats
{
    public int FilesCopied { get; set; }
    public long TotalBytes { get; set; }
    public int DirectoriesCreated { get; set; }
    public Dictionary<string, int> CountByExtension { get; } = new(StringComparer.OrdinalIgnoreCase);

    public override string ToString()
    {
        var extensions = CountByExtension.Count > 0
            ? string.Join(", ", CountByExtension.Select(kv => $"{kv.Key}: {kv.Value}"))
            : "none";
        var mb = TotalBytes / (1024.0 * 1024.0);
        return $"Files: {FilesCopied} ({mb:F2} MB), Directories: {DirectoriesCreated} | Types: {extensions}";
    }
}

/// <summary>
/// Statistics from rewriting image URLs in migration data.
/// </summary>
public sealed class ImageUrlRewriteStats
{
    public int RowsScanned { get; set; }
    public int UrlsRewritten { get; set; }

    public override string ToString() =>
        $"Rows scanned: {RowsScanned}, URLs rewritten: {UrlsRewritten}";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
