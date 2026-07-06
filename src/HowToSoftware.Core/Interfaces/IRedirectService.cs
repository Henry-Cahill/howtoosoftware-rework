using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IRedirectService
{
    Task<List<Redirect>> GetAllAsync(CancellationToken ct = default);
    Task<Redirect?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Redirect?> GetByFromAsync(string fromUrl, CancellationToken ct = default);

    /// <summary>
    /// Resolves the matching redirect for the given request path. Tries exact
    /// (non-regex) matches first, then evaluates regex redirects in stable order.
    /// Returns null if no redirect matches.
    /// </summary>
    Task<RedirectMatch?> MatchAsync(string path, CancellationToken ct = default);

    Task<Redirect> CreateAsync(CreateRedirectRequest request, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateRedirectRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task IncrementHitCountAsync(string id, CancellationToken ct = default);
    Task<BulkImportRedirectsResult> BulkImportAsync(
        string csvContent, bool overwriteExisting = false, CancellationToken ct = default);

    /// <summary>
    /// Detects whether a proposed redirect (<paramref name="from"/> → <paramref name="to"/>)
    /// would create a redirect chain — i.e. the target <paramref name="to"/> is itself
    /// the source of an existing exact (non-regex) redirect. Walks the chain forward up
    /// to a fixed depth to compute a flattened target. Returns <c>null</c> when no chain
    /// is detected. Regex redirects are not considered for chain detection because their
    /// matches depend on the actual request path.
    /// </summary>
    /// <param name="from">The proposed new redirect's source path (used only for cycle detection).</param>
    /// <param name="to">The proposed new redirect's target path.</param>
    /// <param name="excludeId">Optional id to exclude (when editing an existing redirect).</param>
    Task<RedirectChainInfo?> DetectChainAsync(
        string from, string to, string? excludeId = null, CancellationToken ct = default);
}

public record CreateRedirectRequest
{
    public required string From { get; init; }
    public required string To { get; init; }
    public bool IsRegex { get; init; }
}

public record UpdateRedirectRequest
{
    public string? From { get; init; }
    public string? To { get; init; }
    public bool? IsRegex { get; init; }
}

public record BulkImportRedirectsResult
{
    public int Imported { get; init; }
    public int Updated { get; init; }
    public int Skipped { get; init; }
    public List<string> Errors { get; init; } = [];
}

/// <summary>
/// Resolved redirect match for an incoming request path. <see cref="Target"/>
/// is the final destination URL with regex backreferences (e.g. <c>$1</c>)
/// already substituted from the captured groups.
/// </summary>
public record RedirectMatch
{
    public required string Id { get; init; }
    public required string Target { get; init; }
}

/// <summary>
/// Information about a detected redirect chain. The proposed new redirect points at
/// <see cref="IntermediateFrom"/>, which already redirects to <see cref="IntermediateTo"/>;
/// following the chain to its terminal hop yields <see cref="SuggestedTarget"/>.
/// </summary>
public record RedirectChainInfo
{
    /// <summary>The proposed target that is itself the source of an existing redirect.</summary>
    public required string IntermediateFrom { get; init; }

    /// <summary>The destination of the first existing redirect in the chain.</summary>
    public required string IntermediateTo { get; init; }

    /// <summary>The flattened final target after following the chain (terminal hop).</summary>
    public required string SuggestedTarget { get; init; }

    /// <summary>Number of existing redirects traversed (1 = single hop chain).</summary>
    public int HopCount { get; init; }

    /// <summary>True if walking the chain encountered a cycle.</summary>
    public bool CycleDetected { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
