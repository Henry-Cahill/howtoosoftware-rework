using System.Text.RegularExpressions;

namespace HowToSoftware.Web;

/// <summary>
/// Redirects legacy Ghost CMS URL patterns to their .NET equivalents.
/// Handles versioned Ghost Content API paths and other Ghost-specific routes
/// that external consumers or search engine caches may still reference.
/// </summary>
public sealed partial class GhostUrlRedirectMiddleware(RequestDelegate next)
{
    // Ghost Content API: /ghost/api/v3/content/*, /ghost/api/v4/content/*, /ghost/api/v5/content/*
    [GeneratedRegex(@"^/ghost/api/v\d+/content(/.*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GhostVersionedContentApiRegex();

    // Ghost unversioned Content API: /ghost/api/content/*
    [GeneratedRegex(@"^/ghost/api/content(/.*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex GhostUnversionedContentApiRegex();

    // Static redirect map for exact Ghost → .NET path mappings
    private static readonly Dictionary<string, string> ExactRedirects = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/ghost/sitemap.xml"] = "/sitemap.xml",
        ["/sitemap-posts.xml"] = "/sitemap.xml",
        ["/sitemap-pages.xml"] = "/sitemap.xml",
        ["/sitemap-tags.xml"] = "/sitemap.xml",
        ["/sitemap-authors.xml"] = "/sitemap.xml",
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is null
            || (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)))
        {
            await next(context);
            return;
        }

        // 1. Exact redirects
        if (ExactRedirects.TryGetValue(path, out var target))
        {
            Redirect(context, target);
            return;
        }

        // 2. Ghost versioned Content API → /api/content/*
        var versionedMatch = GhostVersionedContentApiRegex().Match(path);
        if (versionedMatch.Success)
        {
            var remainder = versionedMatch.Groups[1].Value;
            Redirect(context, $"/api/content{remainder}");
            return;
        }

        // 3. Ghost unversioned Content API → /api/content/*
        var unversionedMatch = GhostUnversionedContentApiRegex().Match(path);
        if (unversionedMatch.Success)
        {
            var remainder = unversionedMatch.Groups[1].Value;
            Redirect(context, $"/api/content{remainder}");
            return;
        }

        await next(context);
    }

    private static void Redirect(HttpContext context, string target)
    {
        if (context.Request.QueryString.HasValue)
            target += context.Request.QueryString.Value;

        context.Response.StatusCode = 301;
        context.Response.Headers.Location = target;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
