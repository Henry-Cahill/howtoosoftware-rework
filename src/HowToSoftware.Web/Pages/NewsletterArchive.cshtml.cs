using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

public class NewsletterArchiveModel(
    INewsletterRepository newsletterRepository,
    IPostRepository postRepository,
    ISettingsService settings) : PageModel
{
    private const int PageSize = 15;

    public Newsletter Newsletter { get; private set; } = null!;
    public IReadOnlyList<PostCardViewModel> PostCards { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int pageNumber = 1)
    {
        if (pageNumber < 1) pageNumber = 1;

        var newsletter = await newsletterRepository.GetBySlugAsync(slug);
        // Hide the page entirely when the newsletter is missing, archived,
        // or the admin hasn't opted in to a public archive.
        if (newsletter is null
            || newsletter.Status != "active"
            || !newsletter.ArchiveEnabled)
        {
            return NotFound();
        }

        Newsletter = newsletter;

        var result = await postRepository.GetPublishedPostsByNewsletterAsync(newsletter.Id, pageNumber, PageSize);

        if (pageNumber > 1 && pageNumber > result.TotalPages)
            return NotFound();

        PostCards = result.Items.Select(IndexModel.ToPostCard).ToList();
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        HasPreviousPage = result.HasPreviousPage;
        HasNextPage = result.HasNextPage;

        var siteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";

        ViewData["Title"] = $"{newsletter.Name} archive - {siteTitle}";
        ViewData["BodyClass"] = "newsletter-archive-template";
        ViewData["MetaDescription"] = newsletter.Description;
        ViewData["OgTitle"] = $"{newsletter.Name} archive";
        ViewData["OgDescription"] = newsletter.Description;
        ViewData["OgType"] = "website";

        return Page();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
