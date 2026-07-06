using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed class RedirectService(
    AppDbContext db,
    ILogger<RedirectService> logger) : IRedirectService
{
    // Cap regex evaluation to avoid catastrophic backtracking (ReDoS).
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public async Task<List<Redirect>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Redirects
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Redirect?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Redirects.FindAsync([id], ct);
    }

    public async Task<Redirect?> GetByFromAsync(string fromUrl, CancellationToken ct = default)
    {
        return await db.Redirects
            .FirstOrDefaultAsync(r => r.From == fromUrl, ct);
    }

    public async Task<Redirect> CreateAsync(
        CreateRedirectRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var redirect = new Redirect
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            From = request.From.Trim(),
            To = request.To.Trim(),
            IsRegex = request.IsRegex,
            CreatedAt = now,
            UpdatedAt = now,
        };

        if (redirect.IsRegex)
            ValidateRegexPattern(redirect.From);

        db.Redirects.Add(redirect);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created redirect {RedirectId}: {From} → {To}",
            redirect.Id, redirect.From, redirect.To);

        return redirect;
    }

    public async Task UpdateAsync(
        string id, UpdateRedirectRequest request, CancellationToken ct = default)
    {
        var redirect = await db.Redirects.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Redirect {id} not found");

        if (request.From is not null)
            redirect.From = request.From.Trim();

        if (request.To is not null)
            redirect.To = request.To.Trim();

        if (request.IsRegex.HasValue)
            redirect.IsRegex = request.IsRegex.Value;

        if (redirect.IsRegex)
            ValidateRegexPattern(redirect.From);

        redirect.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated redirect {RedirectId}: {From} → {To}",
            redirect.Id, redirect.From, redirect.To);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var deleted = await db.Redirects
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new InvalidOperationException($"Redirect {id} not found");

        logger.LogInformation("Deleted redirect {RedirectId}", id);
    }

    public async Task IncrementHitCountAsync(string id, CancellationToken ct = default)
    {
        await db.Redirects
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.HitCount, r => r.HitCount + 1), ct);
    }

    // Hard cap on chain traversal to prevent unbounded walks if data ever
    // becomes pathological. 10 hops is far beyond anything reasonable.
    private const int ChainWalkMaxDepth = 10;

    public async Task<RedirectChainInfo?> DetectChainAsync(
        string from, string to, string? excludeId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return null;

        var fromTrim = from.Trim();
        var toTrim = to.Trim();

        // Look for an existing exact (non-regex) redirect whose source is the
        // proposed target. Regex redirects are excluded because their matches
        // depend on the actual incoming request path, not a static value.
        var firstHop = await db.Redirects
            .Where(r => !r.IsRegex && r.From == toTrim && (excludeId == null || r.Id != excludeId))
            .Select(r => new { r.Id, r.From, r.To })
            .FirstOrDefaultAsync(ct);

        if (firstHop is null)
            return null;

        // Walk forward up to ChainWalkMaxDepth hops collecting visited sources
        // so we can detect cycles. The starting "visited" set seeds with the
        // proposed new redirect's source so a chain that eventually points
        // back at A is recognised as a cycle.
        var visited = new HashSet<string>(StringComparer.Ordinal) { fromTrim, firstHop.From };
        var currentTarget = firstHop.To;
        var hops = 1;
        var cycleDetected = false;

        while (hops < ChainWalkMaxDepth)
        {
            if (visited.Contains(currentTarget))
            {
                cycleDetected = true;
                break;
            }

            var next = await db.Redirects
                .Where(r => !r.IsRegex && r.From == currentTarget && (excludeId == null || r.Id != excludeId))
                .Select(r => new { r.From, r.To })
                .FirstOrDefaultAsync(ct);

            if (next is null) break;

            visited.Add(next.From);
            currentTarget = next.To;
            hops++;
        }

        return new RedirectChainInfo
        {
            IntermediateFrom = firstHop.From,
            IntermediateTo = firstHop.To,
            SuggestedTarget = currentTarget,
            HopCount = hops,
            CycleDetected = cycleDetected,
        };
    }

    public async Task<RedirectMatch?> MatchAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // 1. Exact (non-regex) match — single-row indexed lookup.
        var exact = await db.Redirects
            .Where(r => !r.IsRegex && r.From == path)
            .Select(r => new { r.Id, r.To })
            .FirstOrDefaultAsync(ct);
        if (exact is not null)
            return new RedirectMatch { Id = exact.Id, Target = exact.To };

        // 2. Regex matches — evaluate in stable order. Patterns are usually few.
        var regexRedirects = await db.Redirects
            .Where(r => r.IsRegex)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.Id, r.From, r.To })
            .ToListAsync(ct);

        foreach (var r in regexRedirects)
        {
            try
            {
                var rx = new Regex(r.From, RegexOptions.CultureInvariant, RegexTimeout);
                var match = rx.Match(path);
                if (!match.Success) continue;
                var target = match.Result(r.To);
                return new RedirectMatch { Id = r.Id, Target = target };
            }
            catch (RegexMatchTimeoutException)
            {
                logger.LogWarning(
                    "Regex redirect {RedirectId} timed out matching path {Path}", r.Id, path);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex,
                    "Regex redirect {RedirectId} has invalid pattern or replacement", r.Id);
            }
        }

        return null;
    }

    private static void ValidateRegexPattern(string pattern)
    {
        // Compile once with a timeout to surface invalid patterns early.
        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, RegexTimeout);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"Invalid regex pattern: {ex.Message}", ex);
        }
    }

    public async Task<BulkImportRedirectsResult> BulkImportAsync(
        string csvContent, bool overwriteExisting = false, CancellationToken ct = default)
    {
        var rows = CsvParser.Parse(csvContent, out var parseErrors);

        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>(parseErrors);

        // Load existing once so we can match without N round-trips.
        var existing = await db.Redirects.ToDictionaryAsync(r => r.From, ct);

        // De-duplicate within the file (last occurrence wins) so we don't try to insert two of the same From.
        var seenFrom = new HashSet<string>(StringComparer.Ordinal);
        var now = DateTime.UtcNow;

        foreach (var (lineNumber, from, to) in rows)
        {
            var fromTrim = from.Trim();
            var toTrim = to.Trim();

            if (string.IsNullOrEmpty(fromTrim) || string.IsNullOrEmpty(toTrim))
            {
                errors.Add($"Line {lineNumber}: from and to are required.");
                skipped++;
                continue;
            }

            if (!seenFrom.Add(fromTrim))
            {
                errors.Add($"Line {lineNumber}: duplicate 'from' value '{fromTrim}' in import — skipped.");
                skipped++;
                continue;
            }

            if (existing.TryGetValue(fromTrim, out var current))
            {
                if (overwriteExisting)
                {
                    if (current.To != toTrim)
                    {
                        current.To = toTrim;
                        current.UpdatedAt = now;
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            var redirect = new Redirect
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                From = fromTrim,
                To = toTrim,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.Redirects.Add(redirect);
            existing[fromTrim] = redirect;
            imported++;
        }

        if (imported > 0 || updated > 0)
            await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Bulk imported redirects: {Imported} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
            imported, updated, skipped, errors.Count);

        return new BulkImportRedirectsResult
        {
            Imported = imported,
            Updated = updated,
            Skipped = skipped,
            Errors = errors,
        };
    }
}

internal static class CsvParser
{
    /// <summary>
    /// Parses a 2-column CSV (from_url, to_url). Detects an optional header row.
    /// Supports quoted fields with double-quote escaping. Returns rows with their
    /// 1-based line number for error reporting.
    /// </summary>
    public static List<(int LineNumber, string From, string To)> Parse(
        string content, out List<string> errors)
    {
        errors = [];
        var rows = new List<(int, string, string)>();

        if (string.IsNullOrWhiteSpace(content))
            return rows;

        // Normalize line endings.
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var checkedHeader = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseLine(line);

            if (!checkedHeader)
            {
                checkedHeader = true;
                if (fields.Count >= 2 && IsHeader(fields[0]) && IsHeader(fields[1]))
                    continue;
            }

            if (fields.Count < 2)
            {
                errors.Add($"Line {lineNumber}: expected 2 columns (from_url, to_url), got {fields.Count}.");
                continue;
            }

            rows.Add((lineNumber, fields[0], fields[1]));
        }

        return rows;
    }

    private static bool IsHeader(string field)
    {
        var v = field.Trim().ToLowerInvariant();
        return v is "from" or "from_url" or "fromurl" or "source"
            or "to" or "to_url" or "tourl" or "destination" or "target";
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
