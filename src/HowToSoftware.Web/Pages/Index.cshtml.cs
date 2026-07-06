using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

public class IndexModel(IPostRepository postRepository, ISettingsService settings) : PageModel
{
    private const int PageSize = 15;

    public IReadOnlyList<PostCardViewModel> PostCards { get; private set; } = [];
    public IReadOnlyList<PostCardViewModel> PageCards { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }
    public string SiteTitle { get; private set; } = "";
    public string SiteDescription { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        if (pageNumber < 1) pageNumber = 1;

        var result = await postRepository.GetPublishedPostsAsync(pageNumber, PageSize);

        if (pageNumber > 1 && pageNumber > result.TotalPages)
            return NotFound();

        var posts = result.Items;
        PostCards = posts.Select(ToPostCard).ToList();
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        HasPreviousPage = result.HasPreviousPage;
        HasNextPage = result.HasNextPage;

        var pages = await postRepository.GetPublishedPagesAsync();
        PageCards = pages.Select(ToPostCard).ToList();

        SiteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";
        SiteDescription = await settings.GetStringAsync("description") ?? "";

        ViewData["BodyClass"] = "home-template";

        return Page();
    }

    public static int EstimateReadingTime(Post post)
    {
        var text = post.Plaintext;
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var wordCount = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = (int)Math.Ceiling(wordCount / 275.0);
        return Math.Max(1, minutes);
    }

    public static string? GetExcerpt(Post post, int maxLength = 150)
    {
        var text = post.CustomExcerpt ?? post.Plaintext;
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (text.Length <= maxLength) return text;
        return string.Concat(text.AsSpan(0, maxLength).TrimEnd(), "\u2026");
    }

    public static PostCardViewModel ToPostCard(Post post)
    {
        var primaryTag = post.PostsTags
            .OrderBy(pt => pt.SortOrder)
            .Select(pt => pt.Tag)
            .FirstOrDefault(t => t.Visibility == "public");

        return new PostCardViewModel
        {
            Slug = post.Slug,
            Title = post.Title,
            FeatureImage = post.FeatureImage,
            FeatureImageAlt = post.Meta?.FeatureImageAlt,
            Visibility = post.Visibility,
            Featured = post.Featured,
            PrimaryTagName = primaryTag?.Name,
            Excerpt = GetExcerpt(post),
            PublishedAt = post.PublishedAt,
            ReadingTimeMinutes = EstimateReadingTime(post),
            CssClasses = "post-card"
                + (post.Featured ? " featured" : "")
                + " keep-ratio"
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
