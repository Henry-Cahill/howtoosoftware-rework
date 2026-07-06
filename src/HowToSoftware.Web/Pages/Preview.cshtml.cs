using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class PreviewModel(
    IPostRepository postRepository,
    ILexicalRenderer lexicalRenderer,
    IMobiledocRenderer mobiledocRenderer,
    ISettingsService settings,
    IContentSanitizer htmlSanitizer) : PageModel
{
    public Post Post { get; private set; } = null!;
    public string RenderedContent { get; private set; } = "";
    public string SiteTitle { get; private set; } = "";
    public bool IsPage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string uuid)
    {
        var post = await postRepository.GetByUuidAsync(uuid);

        if (post is null)
            return NotFound();

        Post = post;
        IsPage = post.Type == "page";

        RenderedContent = htmlSanitizer.Sanitize(post.Html ?? RenderContent(post));

        SiteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";

        ViewData["Title"] = post.Meta?.MetaTitle ?? post.Title;
        ViewData["BodyClass"] = IsPage ? "page-template" : "post-template";
        ViewData["IsPost"] = true;

        return Page();
    }

    private string RenderContent(Post post)
    {
        if (!string.IsNullOrEmpty(post.Lexical))
            return lexicalRenderer.Render(post.Lexical);

        if (!string.IsNullOrEmpty(post.Mobiledoc))
            return mobiledocRenderer.Render(post.Mobiledoc);

        return "";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
