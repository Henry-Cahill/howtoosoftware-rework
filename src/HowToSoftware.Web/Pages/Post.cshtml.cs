using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Models;

namespace HowToSoftware.Web.Pages;

public class PostModel(
    IPostRepository postRepository,
    ILexicalRenderer lexicalRenderer,
    IMobiledocRenderer mobiledocRenderer,
    ISettingsService settings,
    IContentGatingService contentGating,
    IMentionService mentionService,
    IContentSanitizer htmlSanitizer) : PageModel
{
    public Post Post { get; private set; } = null!;
    public string RenderedContent { get; private set; } = "";
    public IReadOnlyList<PostCardViewModel> RelatedPosts { get; private set; } = [];
    public IReadOnlyList<Mention> Mentions { get; private set; } = [];
    public string SiteTitle { get; private set; } = "";
    public bool IsPage { get; private set; }
    public bool ShowTitleAndFeatureImage { get; private set; } = true;
    public ContentAccessLevel AccessLevel { get; private set; } = ContentAccessLevel.Full;
    public bool IsContentGated => AccessLevel != ContentAccessLevel.Full;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var post = await postRepository.GetBySlugAsync(slug);

        if (post is null || post.Status != "published" || (post.Type != "post" && post.Type != "page"))
            return NotFound();

        IsPage = post.Type == "page";

        Post = post;

        // Check content gating: does this visitor have access?
        var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var memberIdentity = User.Identities.FirstOrDefault(i => i.AuthenticationType == "MemberCookie");
        var effectiveMemberId = memberIdentity?.IsAuthenticated == true ? memberId : null;
        AccessLevel = await contentGating.CheckAccessAsync(post, effectiveMemberId);

        if (IsContentGated)
        {
            // Show excerpt only — don't render full content
            RenderedContent = "";
        }
        else
        {
            // Render content from Lexical or Mobiledoc JSON if Html isn't pre-rendered
            RenderedContent = htmlSanitizer.Sanitize(post.Html ?? RenderContent(post));
        }

        // Load related posts (same tags, excluding current) — posts only
        if (!IsPage)
        {
            var related = await postRepository.GetRelatedPostsAsync(post.Id, 3);
            RelatedPosts = related.Select(IndexModel.ToPostCard).ToList();
        }

        // Load verified webmentions for this post
        Mentions = await mentionService.GetByResourceAsync(post.Id, post.Type);

        SiteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";

        // SEO metadata
        ViewData["Title"] = post.Meta?.MetaTitle ?? post.Title;
        ViewData["BodyClass"] = IsPage ? "page-template" : "post-template";
        ViewData["IsPost"] = true;
        ViewData["MetaDescription"] = post.Meta?.MetaDescription ?? post.CustomExcerpt;
        ViewData["OgTitle"] = post.Meta?.OgTitle ?? post.Title;
        ViewData["OgDescription"] = post.Meta?.OgDescription ?? post.CustomExcerpt;
        ViewData["OgImage"] = post.Meta?.OgImage ?? post.FeatureImage;
        ViewData["OgType"] = "article";
        ViewData["CanonicalUrl"] = post.CanonicalUrl;

        return Page();
    }

    public IReadOnlyList<Tag> PublicTags =>
        Post.PostsTags
            .OrderBy(pt => pt.SortOrder)
            .Select(pt => pt.Tag)
            .Where(t => t.Visibility == "public")
            .ToList();

    public Tag? PrimaryTag => PublicTags.FirstOrDefault();

    public IReadOnlyList<User> Authors =>
        Post.PostsAuthors
            .OrderBy(pa => pa.SortOrder)
            .Select(pa => pa.Author)
            .ToList();

    public int ReadingTimeMinutes => IndexModel.EstimateReadingTime(Post);

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
