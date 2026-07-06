using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface ISnippetService
{
    Task<List<Snippet>> GetAllAsync(CancellationToken ct = default);
    Task<Snippet?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Snippet?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Snippet> CreateAsync(CreateSnippetRequest request, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateSnippetRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of posts whose Lexical or Mobiledoc content references each snippet
    /// (matched by snippet name as a substring). Keys are snippet IDs.
    /// </summary>
    Task<Dictionary<string, int>> GetUsageCountsAsync(CancellationToken ct = default);
}

public record CreateSnippetRequest
{
    public required string Name { get; init; }
    public required string Mobiledoc { get; init; }
    public string? Lexical { get; init; }
}

public record UpdateSnippetRequest
{
    public string? Name { get; init; }
    public string? Mobiledoc { get; init; }
    public string? Lexical { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
