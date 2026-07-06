using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

public class AuthorModel(
    IUserRepository userRepository,
    IPostRepository postRepository,
    ISettingsService settings) : PageModel
{
    private const int PageSize = 15;

    public User Author { get; private set; } = null!;
    public int PostCount { get; private set; }
    public IReadOnlyList<PostCardViewModel> PostCards { get; private set; } = [];
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public bool HasPreviousPage { get; private set; }
    public bool HasNextPage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int pageNumber = 1)
    {
        if (pageNumber < 1) pageNumber = 1;

        var author = await userRepository.GetBySlugAsync(slug);
        if (author is null || author.Status != "active")
            return NotFound();

        Author = author;
        PostCount = await userRepository.GetPostCountAsync(author.Id);

        var result = await postRepository.GetPublishedPostsByAuthorAsync(slug, pageNumber, PageSize);

        if (pageNumber > 1 && pageNumber > result.TotalPages)
            return NotFound();

        PostCards = result.Items.Select(IndexModel.ToPostCard).ToList();
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        HasPreviousPage = result.HasPreviousPage;
        HasNextPage = result.HasNextPage;

        var siteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";

        ViewData["Title"] = $"{author.Name} - {siteTitle}";
        ViewData["BodyClass"] = "author-template";
        ViewData["MetaDescription"] = author.MetaDescription ?? author.Bio;
        ViewData["OgTitle"] = author.MetaTitle ?? author.Name;
        ViewData["OgDescription"] = author.MetaDescription ?? author.Bio;
        ViewData["OgImage"] = author.CoverImage ?? author.ProfileImage;
        ViewData["OgType"] = "profile";

        return Page();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
