using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public partial class SlugGenerator : ISlugGenerator
{
    private const int MaxSlugLength = 191;

    public string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize unicode (decompose accented chars) and strip combining marks
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();

        // Replace unsafe characters (keep letters, digits, whitespace, hyphens)
        slug = UnsafeChars().Replace(slug, "");

        // Collapse whitespace to single hyphen
        slug = Whitespace().Replace(slug, "-");

        // Collapse multiple consecutive hyphens
        slug = MultipleDashes().Replace(slug, "-");

        // Trim leading/trailing hyphens
        slug = slug.Trim('-');

        // Enforce max length without cutting mid-word when possible
        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength];
            var lastDash = slug.LastIndexOf('-');
            if (lastDash > MaxSlugLength / 2)
                slug = slug[..lastDash];
        }

        return slug;
    }

    public async Task<string> GenerateUniqueSlugAsync(
        string text,
        Func<string, Task<bool>> slugExistsAsync,
        CancellationToken ct = default)
    {
        var baseSlug = GenerateSlug(text);
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "untitled";

        if (!await slugExistsAsync(baseSlug))
            return baseSlug;

        for (var i = 2; i <= 1000; i++)
        {
            ct.ThrowIfCancellationRequested();
            var candidate = $"{baseSlug}-{i}";

            // Ensure suffixed slug still fits within max length
            if (candidate.Length > MaxSlugLength)
            {
                var suffixLength = $"-{i}".Length;
                candidate = $"{baseSlug[..(MaxSlugLength - suffixLength)]}-{i}";
            }

            if (!await slugExistsAsync(candidate))
                return candidate;
        }

        // Fallback: append GUID segment
        return $"{baseSlug[..Math.Min(baseSlug.Length, MaxSlugLength - 9)]}-{Guid.NewGuid().ToString("N")[..8]}";
    }

    [GeneratedRegex(@"[^\p{L}\p{N}\s\-]")]
    private static partial Regex UnsafeChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleDashes();
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
