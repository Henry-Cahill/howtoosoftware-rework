using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly IPostRepository _postRepository;

    public ErrorModel(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    // Default to 500 so the page never renders as "Error 0" if the
    // handler is skipped (e.g. when UseExceptionHandler re-executes a POST
    // and Razor Pages can't find a matching handler for this page).
    public int ErrorStatusCode { get; set; } = 500;
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public IReadOnlyList<PostCardViewModel> SuggestedPosts { get; set; } = [];

    public Task OnGetAsync(int? statusCode) => HandleAsync(statusCode);

    // UseExceptionHandler preserves the original HTTP method when re-executing,
    // so a POST that throws will re-dispatch to /Error as a POST. Without this
    // handler, no page handler runs and ErrorStatusCode stays at its default.
    public Task OnPostAsync(int? statusCode) => HandleAsync(statusCode);

    private async Task HandleAsync(int? statusCode)
    {
        ErrorStatusCode = statusCode ?? HttpContext.Response.StatusCode;
        if (ErrorStatusCode is 0 or 200) ErrorStatusCode = 500;
        HttpContext.Response.StatusCode = ErrorStatusCode;
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        if (ErrorStatusCode == 404)
        {
            var result = await _postRepository.GetPublishedPostsAsync(1, 3);
            SuggestedPosts = result.Items.Select(IndexModel.ToPostCard).ToList();
        }
    }
}


// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
