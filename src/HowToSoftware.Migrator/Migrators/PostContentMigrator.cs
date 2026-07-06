namespace HowToSoftware.Migrator;

/// <summary>
/// Handles post/page content migration from Ghost MySQL dumps.
/// Rewrites Ghost-specific URL placeholders, and collects migration statistics
/// for posts, pages, authors, and tags.
/// </summary>
public static class PostContentMigrator
{
    /// <summary>
    /// Ghost uses __GHOST_URL__ as a placeholder in stored content (Lexical JSON, Mobiledoc JSON, HTML)
    /// that gets replaced at render time with the site's configured URL.
    /// During migration we replace this with the new site URL.
    /// </summary>
    private const string GhostUrlPlaceholder = "__GHOST_URL__";

    /// <summary>
    /// Content columns in the posts table that may contain __GHOST_URL__ references.
    /// </summary>
    private static readonly HashSet<string> ContentColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "mobiledoc", "lexical", "html", "plaintext",
        "feature_image", "canonical_url",
        "codeinjection_head", "codeinjection_foot",
    };

    /// <summary>
    /// Post-related tables that should be included in content migration.
    /// </summary>
    private static readonly HashSet<string> PostContentTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "posts", "posts_tags", "posts_authors", "posts_meta",
        "post_revisions", "mobiledoc_revisions",
    };

    /// <summary>
    /// Processes parsed INSERT statements to apply content migration transforms.
    /// Rewrites __GHOST_URL__ placeholders to the new site URL in content columns.
    /// Returns the transformed inserts and migration statistics.
    /// </summary>
    /// <param name="inserts">Parsed INSERT statements from the MySQL dump.</param>
    /// <param name="newSiteUrl">The new site URL to replace __GHOST_URL__ with (e.g., "https://howtoosoftware.com").</param>
    /// <returns>Transformed inserts and content migration statistics.</returns>
    public static ContentMigrationResult ProcessContent(
        IReadOnlyList<ParsedInsert> inserts, string newSiteUrl)
    {
        var siteUrl = newSiteUrl.TrimEnd('/');
        var transformed = new List<ParsedInsert>(inserts.Count);
        var stats = new ContentMigrationStats();

        foreach (var insert in inserts)
        {
            if (insert.TableName.Equals("posts", StringComparison.OrdinalIgnoreCase))
            {
                var transformedInsert = TransformPosts(insert, siteUrl, stats);
                transformed.Add(transformedInsert);
            }
            else if (PostContentTables.Contains(insert.TableName))
            {
                // Track join table counts
                TrackJoinTable(insert, stats);
                var transformedInsert = TransformContentColumns(insert, siteUrl);
                transformed.Add(transformedInsert);
            }
            else
            {
                transformed.Add(insert);
            }
        }

        return new ContentMigrationResult(transformed, stats);
    }

    /// <summary>
    /// Transforms the posts table: rewrites URLs and collects post/page statistics.
    /// </summary>
    private static ParsedInsert TransformPosts(ParsedInsert insert, string siteUrl, ContentMigrationStats stats)
    {
        var typeColIdx = Array.FindIndex(insert.Columns, c => c.Equals("type", StringComparison.OrdinalIgnoreCase));
        var statusColIdx = Array.FindIndex(insert.Columns, c => c.Equals("status", StringComparison.OrdinalIgnoreCase));
        var lexicalColIdx = Array.FindIndex(insert.Columns, c => c.Equals("lexical", StringComparison.OrdinalIgnoreCase));
        var mobiledocColIdx = Array.FindIndex(insert.Columns, c => c.Equals("mobiledoc", StringComparison.OrdinalIgnoreCase));

        var newRows = new List<string?[]>(insert.Rows.Count);

        foreach (var row in insert.Rows)
        {
            // Collect statistics
            var type = typeColIdx >= 0 ? row[typeColIdx] : null;
            var status = statusColIdx >= 0 ? row[statusColIdx] : null;

            if (string.Equals(type, "page", StringComparison.OrdinalIgnoreCase))
                stats.PageCount++;
            else
                stats.PostCount++;

            if (string.Equals(status, "published", StringComparison.OrdinalIgnoreCase))
                stats.PublishedCount++;
            else if (string.Equals(status, "draft", StringComparison.OrdinalIgnoreCase))
                stats.DraftCount++;

            // Detect content format
            var hasLexical = lexicalColIdx >= 0 && row[lexicalColIdx] is not null;
            var hasMobiledoc = mobiledocColIdx >= 0 && row[mobiledocColIdx] is not null;

            if (hasLexical)
                stats.LexicalCount++;
            if (hasMobiledoc)
                stats.MobiledocCount++;

            // Rewrite URLs in content columns
            var newRow = RewriteRowUrls(insert.Columns, row, siteUrl);
            newRows.Add(newRow);
        }

        return new ParsedInsert(insert.TableName, insert.Columns, newRows);
    }

    /// <summary>
    /// Tracks join table row counts for migration statistics.
    /// </summary>
    private static void TrackJoinTable(ParsedInsert insert, ContentMigrationStats stats)
    {
        if (insert.TableName.Equals("posts_tags", StringComparison.OrdinalIgnoreCase))
            stats.PostsTagsCount += insert.Rows.Count;
        else if (insert.TableName.Equals("posts_authors", StringComparison.OrdinalIgnoreCase))
            stats.PostsAuthorsCount += insert.Rows.Count;
        else if (insert.TableName.Equals("posts_meta", StringComparison.OrdinalIgnoreCase))
            stats.PostsMetaCount += insert.Rows.Count;
        else if (insert.TableName.Equals("post_revisions", StringComparison.OrdinalIgnoreCase))
            stats.PostRevisionsCount += insert.Rows.Count;
        else if (insert.TableName.Equals("mobiledoc_revisions", StringComparison.OrdinalIgnoreCase))
            stats.MobiledocRevisionsCount += insert.Rows.Count;
    }

    /// <summary>
    /// Transforms content columns in non-posts tables (e.g., post_revisions with lexical content).
    /// </summary>
    private static ParsedInsert TransformContentColumns(ParsedInsert insert, string siteUrl)
    {
        // Check if any column is a content column worth rewriting
        var hasContentCols = insert.Columns.Any(c =>
            ContentColumns.Contains(c) ||
            c.Equals("lexical", StringComparison.OrdinalIgnoreCase) ||
            c.Equals("mobiledoc", StringComparison.OrdinalIgnoreCase));

        if (!hasContentCols)
            return insert;

        var newRows = new List<string?[]>(insert.Rows.Count);
        foreach (var row in insert.Rows)
        {
            newRows.Add(RewriteRowUrls(insert.Columns, row, siteUrl));
        }
        return new ParsedInsert(insert.TableName, insert.Columns, newRows);
    }

    /// <summary>
    /// Rewrites __GHOST_URL__ placeholders in content columns of a single row.
    /// </summary>
    private static string?[] RewriteRowUrls(string[] columns, string?[] row, string siteUrl)
    {
        var newRow = new string?[row.Length];
        Array.Copy(row, newRow, row.Length);

        for (var i = 0; i < columns.Length && i < newRow.Length; i++)
        {
            if (newRow[i] is not null && ContentColumns.Contains(columns[i]))
            {
                newRow[i] = RewriteGhostUrls(newRow[i]!, siteUrl);
            }
        }

        return newRow;
    }

    /// <summary>
    /// Replaces all __GHOST_URL__ occurrences in a string with the new site URL.
    /// Handles both escaped and unescaped variants found in Ghost content.
    /// </summary>
    internal static string RewriteGhostUrls(string content, string siteUrl)
    {
        if (!content.Contains(GhostUrlPlaceholder, StringComparison.Ordinal))
            return content;

        return content.Replace(GhostUrlPlaceholder, siteUrl, StringComparison.Ordinal);
    }

    /// <summary>
    /// Filters a list of parsed inserts to only include post-content-related tables.
    /// </summary>
    public static IReadOnlyList<ParsedInsert> FilterPostContentTables(IEnumerable<ParsedInsert> inserts)
    {
        return inserts
            .Where(i => PostContentTables.Contains(i.TableName))
            .ToList();
    }

    /// <summary>
    /// Checks if a table name is a post-content-related table.
    /// </summary>
    public static bool IsPostContentTable(string tableName)
        => PostContentTables.Contains(tableName);
}

/// <summary>
/// Result of content migration processing.
/// </summary>
public sealed record ContentMigrationResult(
    IReadOnlyList<ParsedInsert> TransformedInserts,
    ContentMigrationStats Stats);

/// <summary>
/// Statistics collected during content migration.
/// </summary>
public sealed class ContentMigrationStats
{
    public int PostCount { get; set; }
    public int PageCount { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int LexicalCount { get; set; }
    public int MobiledocCount { get; set; }
    public int PostsTagsCount { get; set; }
    public int PostsAuthorsCount { get; set; }
    public int PostsMetaCount { get; set; }
    public int PostRevisionsCount { get; set; }
    public int MobiledocRevisionsCount { get; set; }

    public int TotalContentItems => PostCount + PageCount;

    public override string ToString() =>
        $"Posts: {PostCount}, Pages: {PageCount} ({PublishedCount} published, {DraftCount} drafts) | " +
        $"Lexical: {LexicalCount}, Mobiledoc: {MobiledocCount} | " +
        $"Tags: {PostsTagsCount}, Authors: {PostsAuthorsCount}, Meta: {PostsMetaCount}, " +
        $"Revisions: {PostRevisionsCount}, MobiledocRevisions: {MobiledocRevisionsCount}";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
