using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed class SnippetService(
    AppDbContext db,
    ILogger<SnippetService> logger) : ISnippetService
{
    public async Task<List<Snippet>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Snippets
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Snippet?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Snippets.FindAsync([id], ct);
    }

    public async Task<Snippet?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await db.Snippets
            .FirstOrDefaultAsync(s => s.Name == name, ct);
    }

    public async Task<Snippet> CreateAsync(
        CreateSnippetRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var snippet = new Snippet
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Name = request.Name.Trim(),
            Mobiledoc = request.Mobiledoc,
            Lexical = request.Lexical,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Snippets.Add(snippet);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created snippet {SnippetId}: {Name}", snippet.Id, snippet.Name);

        return snippet;
    }

    public async Task UpdateAsync(
        string id, UpdateSnippetRequest request, CancellationToken ct = default)
    {
        var snippet = await db.Snippets.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Snippet {id} not found");

        if (request.Name is not null)
            snippet.Name = request.Name.Trim();

        if (request.Mobiledoc is not null)
            snippet.Mobiledoc = request.Mobiledoc;

        if (request.Lexical is not null)
            snippet.Lexical = request.Lexical;

        snippet.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated snippet {SnippetId}: {Name}", snippet.Id, snippet.Name);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var deleted = await db.Snippets
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new InvalidOperationException($"Snippet {id} not found");

        logger.LogInformation("Deleted snippet {SnippetId}", LogSanitizer.SanitizeForLog(id));
    }

    public async Task<Dictionary<string, int>> GetUsageCountsAsync(CancellationToken ct = default)
    {
        var snippets = await db.Snippets
            .Select(s => new { s.Id, s.Name })
            .ToListAsync(ct);

        var result = new Dictionary<string, int>(snippets.Count);

        foreach (var snippet in snippets)
        {
            if (string.IsNullOrWhiteSpace(snippet.Name))
            {
                result[snippet.Id] = 0;
                continue;
            }

            var pattern = $"%{EscapeLike(snippet.Name)}%";

            var count = await db.Posts
                .Where(p =>
                    (p.Lexical != null && EF.Functions.Like(p.Lexical, pattern))
                    || (p.Mobiledoc != null && EF.Functions.Like(p.Mobiledoc, pattern)))
                .CountAsync(ct);

            result[snippet.Id] = count;
        }

        return result;
    }

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
