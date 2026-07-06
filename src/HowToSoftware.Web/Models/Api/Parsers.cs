namespace HowToSoftware.Web.Models.Api;

/// <summary>
/// Parses Ghost-style filter strings like "tag:getting-started+status:published"
/// into key-value pairs.
/// </summary>
public static class FilterParser
{
    public static Dictionary<string, string> Parse(string? filter)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(filter)) return result;

        // Ghost uses NQL (Nested Query Language). For Content API, we support
        // simple key:value pairs separated by + (AND).
        // Examples: "tag:slug", "author:slug", "status:published", "tag:slug+status:published"
        foreach (var segment in filter.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colonIdx = segment.IndexOf(':');
            if (colonIdx > 0 && colonIdx < segment.Length - 1)
            {
                var key = segment[..colonIdx].Trim();
                var value = segment[(colonIdx + 1)..].Trim();
                result[key] = value;
            }
        }

        return result;
    }
}

/// <summary>
/// Parses the "include" query parameter (comma-separated relation names).
/// </summary>
public static class IncludeParser
{
    private static readonly HashSet<string> AllowedIncludes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tags", "authors"
    };

    public static HashSet<string> Parse(string? include)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(include)) return result;

        foreach (var item in include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (AllowedIncludes.Contains(item))
                result.Add(item);
        }

        return result;
    }
}

/// <summary>
/// Parses the "fields" query parameter (comma-separated field names).
/// Returns null if no fields specified (meaning all fields).
/// </summary>
public static class FieldParser
{
    public static HashSet<string>? Parse(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields)) return null;

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result.Add(field);
        }

        // Always include id
        result.Add("id");

        return result.Count > 1 ? result : null;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
