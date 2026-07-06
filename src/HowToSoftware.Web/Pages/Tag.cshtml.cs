using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

public class TagModel(
    ITagRepository tagRepository,
    IPostRepository postRepository,
    ISettingsService settings) : PageModel
{
    private const int PageSize = 15;

    public Core.Entities.Tag Tag { get; private set; } = null!;
    public int PostCount { get; private set; }
    public IReadOnlyList<PostCardViewModel> PostCards { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int pageNumber = 1)
    {
        if (pageNumber < 1) pageNumber = 1;

        var tag = await tagRepository.GetBySlugAsync(slug);
        if (tag is null || tag.Visibility != "public")
            return NotFound();

        Tag = tag;
        PostCount = await tagRepository.GetPostCountAsync(tag.Id);

        var result = await postRepository.GetPublishedPostsByTagAsync(slug, pageNumber, PageSize);

        if (pageNumber > 1 && pageNumber > result.TotalPages)
            return NotFound();

        PostCards = result.Items.Select(IndexModel.ToPostCard).ToList();
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        HasPreviousPage = result.HasPreviousPage;
        HasNextPage = result.HasNextPage;

        var siteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";

        ViewData["Title"] = $"{tag.Name} - {siteTitle}";
        ViewData["BodyClass"] = "tag-template";
        ViewData["MetaDescription"] = tag.MetaDescription ?? tag.Description;
        ViewData["OgTitle"] = tag.MetaTitle ?? tag.Name;
        ViewData["OgDescription"] = tag.MetaDescription ?? tag.Description;
        ViewData["OgImage"] = tag.OgImage ?? tag.FeatureImage;
        ViewData["OgType"] = "website";

        return Page();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
