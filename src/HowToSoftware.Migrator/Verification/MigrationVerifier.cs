using System.Text;
using System.Text.RegularExpressions;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Migrator;

/// <summary>
/// Verifies migration accuracy by comparing Ghost's original rendered HTML
/// with the clone's re-rendered HTML for every post and page.
/// </summary>
public sealed partial class MigrationVerifier
{
    private readonly LexicalRenderer _lexicalRenderer = new();
    private readonly MobiledocRenderer _mobiledocRenderer = new();

    /// <summary>
    /// Verifies all posts/pages in the parsed inserts by comparing Ghost HTML
    /// against clone-rendered HTML.
    /// </summary>
    /// <param name="inserts">Parsed INSERT statements from the Ghost dump.</param>
    /// <param name="siteUrl">Site URL used to replace __GHOST_URL__ placeholders before rendering.</param>
    /// <returns>Verification results for each post/page.</returns>
    public VerificationReport Verify(IReadOnlyList<ParsedInsert> inserts, string? siteUrl = null)
    {
        var results = new List<PostVerificationResult>();

        foreach (var insert in inserts)
        {
            if (!insert.TableName.Equals("posts", StringComparison.OrdinalIgnoreCase))
                continue;

            var colMap = BuildColumnMap(insert.Columns);

            foreach (var row in insert.Rows)
            {
                var result = VerifyRow(row, colMap, siteUrl);
                results.Add(result);
            }
        }

        return new VerificationReport(results);
    }

    private PostVerificationResult VerifyRow(string?[] row, Dictionary<string, int> colMap, string? siteUrl)
    {
        var id = GetColumn(row, colMap, "id") ?? "(unknown)";
        var title = GetColumn(row, colMap, "title") ?? "(untitled)";
        var slug = GetColumn(row, colMap, "slug") ?? "";
        var type = GetColumn(row, colMap, "type") ?? "post";
        var status = GetColumn(row, colMap, "status") ?? "draft";

        var lexical = UnescapeMySql(GetColumn(row, colMap, "lexical"));
        var mobiledoc = UnescapeMySql(GetColumn(row, colMap, "mobiledoc"));
        var ghostHtml = UnescapeMySql(GetColumn(row, colMap, "html"));

        // Apply URL rewriting to source JSON (same as migration pipeline)
        if (siteUrl is not null)
        {
            if (lexical is not null)
                lexical = PostContentMigrator.RewriteGhostUrls(lexical, siteUrl);
            if (mobiledoc is not null)
                mobiledoc = PostContentMigrator.RewriteGhostUrls(mobiledoc, siteUrl);
            if (ghostHtml is not null)
                ghostHtml = PostContentMigrator.RewriteGhostUrls(ghostHtml, siteUrl);
        }

        // Determine content format and render
        string? cloneHtml = null;
        string format;

        if (lexical is not null)
        {
            format = "lexical";
            try
            {
                cloneHtml = _lexicalRenderer.Render(lexical);
            }
            catch (Exception ex)
            {
                return new PostVerificationResult(
                    id, title, slug, type, status, format,
                    VerificationStatus.RenderError,
                    $"Lexical render failed: {ex.Message}",
                    ghostHtml, null);
            }
        }
        else if (mobiledoc is not null)
        {
            format = "mobiledoc";
            try
            {
                cloneHtml = _mobiledocRenderer.Render(mobiledoc);
            }
            catch (Exception ex)
            {
                return new PostVerificationResult(
                    id, title, slug, type, status, format,
                    VerificationStatus.RenderError,
                    $"Mobiledoc render failed: {ex.Message}",
                    ghostHtml, null);
            }
        }
        else if (ghostHtml is not null)
        {
            // No source JSON — only pre-rendered HTML exists
            format = "html-only";
            return new PostVerificationResult(
                id, title, slug, type, status, format,
                VerificationStatus.HtmlOnly,
                "No Lexical or Mobiledoc source — using pre-rendered HTML as-is",
                ghostHtml, ghostHtml);
        }
        else
        {
            format = "empty";
            return new PostVerificationResult(
                id, title, slug, type, status, format,
                VerificationStatus.Empty,
                "No content (no Lexical, Mobiledoc, or HTML)",
                null, null);
        }

        // Compare normalized HTML
        var normalizedGhost = NormalizeHtml(ghostHtml ?? "");
        var normalizedClone = NormalizeHtml(cloneHtml ?? "");

        if (normalizedGhost == normalizedClone)
        {
            return new PostVerificationResult(
                id, title, slug, type, status, format,
                VerificationStatus.Match,
                null, ghostHtml, cloneHtml);
        }

        // Find first difference position for diagnostic
        var diffDetail = DescribeDifference(normalizedGhost, normalizedClone);

        return new PostVerificationResult(
            id, title, slug, type, status, format,
            VerificationStatus.Mismatch,
            diffDetail, ghostHtml, cloneHtml);
    }

    /// <summary>
    /// Normalizes HTML for comparison. Handles expected differences between
    /// Ghost's renderer and our clone renderer:
    /// - Decodes HTML entities to literal characters
    /// - Collapses whitespace
    /// - Normalizes self-closing tags (br/, hr/, img/)
    /// - Percent-decodes attribute values for consistent comparison
    /// </summary>
    internal static string NormalizeHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        var result = html;

        // Decode HTML entities: &#39; → ', &#160; → \u00A0, &amp; → &, &nbsp; → \u00A0, &quot; → "
        result = System.Net.WebUtility.HtmlDecode(result);

        // Normalize &nbsp; / \u00A0 to regular space for comparison
        result = result.Replace('\u00A0', ' ');

        // Percent-decode attribute values (e.g., %D1%81 → Cyrillic chars in heading IDs)
        result = PercentEncodedInAttr().Replace(result, m =>
        {
            try { return Uri.UnescapeDataString(m.Value); }
            catch { return m.Value; }
        });

        // Strip empty element tags that contain no real content (e.g., <p></p>)
        result = EmptyElements().Replace(result, "");

        // Normalize self-closing tags: <br />, <br/> → <br>
        result = SelfClosingBr().Replace(result, "<br>");
        result = SelfClosingHr().Replace(result, "<hr>");

        // Normalize <img ... /> → <img ...>
        result = SelfClosingImg().Replace(result, "$1>");

        // Collapse whitespace between tags
        result = WhitespaceBetweenTags().Replace(result, "><");

        // Collapse runs of whitespace to single space
        result = MultipleWhitespace().Replace(result, " ");

        // Trim leading/trailing whitespace
        result = result.Trim();

        return result;
    }

    private static string DescribeDifference(string ghost, string clone)
    {
        var minLen = Math.Min(ghost.Length, clone.Length);
        var firstDiff = 0;

        for (var i = 0; i < minLen; i++)
        {
            if (ghost[i] != clone[i])
            {
                firstDiff = i;
                break;
            }
            firstDiff = i + 1;
        }

        if (firstDiff == minLen && ghost.Length != clone.Length)
        {
            var shorter = ghost.Length < clone.Length ? "Ghost" : "Clone";
            var longer = ghost.Length < clone.Length ? "Clone" : "Ghost";
            return $"{shorter} HTML is shorter ({ghost.Length} vs {clone.Length} chars). " +
                   $"{longer} has extra content starting at position {minLen}.";
        }

        var contextStart = Math.Max(0, firstDiff - 40);
        var contextEnd = Math.Min(minLen, firstDiff + 40);

        var ghostContext = ghost[contextStart..Math.Min(ghost.Length, contextEnd)];
        var cloneContext = clone[contextStart..Math.Min(clone.Length, contextEnd)];

        return $"First difference at position {firstDiff}.\n" +
               $"  Ghost: ...{ghostContext}...\n" +
               $"  Clone: ...{cloneContext}...";
    }

    private static Dictionary<string, int> BuildColumnMap(string[] columns)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < columns.Length; i++)
            map[columns[i]] = i;
        return map;
    }

    private static string? GetColumn(string?[] row, Dictionary<string, int> colMap, string column)
    {
        if (colMap.TryGetValue(column, out var idx) && idx < row.Length)
            return row[idx];
        return null;
    }

    /// <summary>
    /// Unescapes MySQL backslash-escaped strings. The MySqlDumpParser preserves
    /// escapes like \' and \\ as-is; we need to convert them back to the original
    /// characters before parsing as JSON or comparing HTML.
    /// </summary>
    internal static string? UnescapeMySql(string? value)
    {
        if (value is null || !value.Contains('\\'))
            return value;

        var sb = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                var next = value[i + 1];
                switch (next)
                {
                    case '\'': sb.Append('\''); i++; break;
                    case '"':  sb.Append('"');  i++; break;
                    case '\\': sb.Append('\\'); i++; break;
                    case 'n':  sb.Append('\n'); i++; break;
                    case 'r':  sb.Append('\r'); i++; break;
                    case 't':  sb.Append('\t'); i++; break;
                    case '0':  sb.Append('\0'); i++; break;
                    default:   sb.Append(value[i]); break;  // preserve unknown escapes
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"<br\s*/?\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex SelfClosingBr();

    [GeneratedRegex(@"<hr\s*/?\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex SelfClosingHr();

    [GeneratedRegex(@"(<img\b[^>]*?)\s*/\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex SelfClosingImg();

    [GeneratedRegex(@">\s+<")]
    private static partial Regex WhitespaceBetweenTags();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleWhitespace();

    [GeneratedRegex(@"(%[0-9A-Fa-f]{2})+")]
    private static partial Regex PercentEncodedInAttr();

    [GeneratedRegex(@"<(p|div|span)>\s*</\1>")]
    private static partial Regex EmptyElements();
}

public enum VerificationStatus
{
    /// <summary>Clone-rendered HTML matches Ghost HTML (after normalization).</summary>
    Match,

    /// <summary>Clone-rendered HTML differs from Ghost HTML.</summary>
    Mismatch,

    /// <summary>Renderer threw an exception processing the source JSON.</summary>
    RenderError,

    /// <summary>Post has only pre-rendered HTML, no source JSON to re-render.</summary>
    HtmlOnly,

    /// <summary>Post has no content at all.</summary>
    Empty,
}

/// <summary>
/// Verification result for a single post or page.
/// </summary>
public sealed record PostVerificationResult(
    string Id,
    string Title,
    string Slug,
    string Type,
    string Status,
    string Format,
    VerificationStatus VerificationStatus,
    string? Detail,
    string? GhostHtml,
    string? CloneHtml);

/// <summary>
/// Full verification report across all posts and pages.
/// </summary>
public sealed class VerificationReport(IReadOnlyList<PostVerificationResult> results)
{
    public IReadOnlyList<PostVerificationResult> Results { get; } = results;

    public int TotalCount => Results.Count;
    public int MatchCount => Results.Count(r => r.VerificationStatus == VerificationStatus.Match);
    public int MismatchCount => Results.Count(r => r.VerificationStatus == VerificationStatus.Mismatch);
    public int RenderErrorCount => Results.Count(r => r.VerificationStatus == VerificationStatus.RenderError);
    public int HtmlOnlyCount => Results.Count(r => r.VerificationStatus == VerificationStatus.HtmlOnly);
    public int EmptyCount => Results.Count(r => r.VerificationStatus == VerificationStatus.Empty);

    public int PostCount => Results.Count(r => r.Type == "post");
    public int PageCount => Results.Count(r => r.Type == "page");

    public override string ToString()
    {
        var lines = new List<string>
        {
            $"Verification Report: {TotalCount} items ({PostCount} posts, {PageCount} pages)",
            $"  Match: {MatchCount} | Mismatch: {MismatchCount} | Render errors: {RenderErrorCount} | HTML-only: {HtmlOnlyCount} | Empty: {EmptyCount}",
        };

        foreach (var r in Results)
        {
            var icon = r.VerificationStatus switch
            {
                VerificationStatus.Match => "OK",
                VerificationStatus.Mismatch => "DIFF",
                VerificationStatus.RenderError => "ERR",
                VerificationStatus.HtmlOnly => "HTML",
                VerificationStatus.Empty => "EMPTY",
                _ => "?",
            };

            lines.Add($"  [{icon}] {r.Type}/{r.Status}: \"{r.Title}\" ({r.Slug}) [{r.Format}]");
            if (r.Detail is not null)
            {
                foreach (var detailLine in r.Detail.Split('\n'))
                    lines.Add($"         {detailLine}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
