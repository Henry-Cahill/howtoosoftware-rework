namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Generates URL-safe slugs from text, with duplicate detection.
/// </summary>
public interface ISlugGenerator
{
    /// <summary>
    /// Generates a URL-safe slug from the given text.
    /// Does not check for duplicates — returns the base slug only.
    /// </summary>
    string GenerateSlug(string text);

    /// <summary>
    /// Generates a unique slug by appending -2, -3, etc. if the base slug
    /// already exists according to the provided check function.
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string text, Func<string, Task<bool>> slugExistsAsync, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
