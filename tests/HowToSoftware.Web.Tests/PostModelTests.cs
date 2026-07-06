using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;

namespace HowToSoftware.Web.Tests;

public class PostModelTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly FakeLexicalRenderer _lexical = new();
    private readonly FakeMobiledocRenderer _mobiledoc = new();
    private readonly FakeSettingsService _settings = new();
    private readonly FakeContentGatingService _gating = new();
    private readonly FakeMentionService _mentions = new();
    private readonly FakeContentSanitizer _sanitizer = new();

    private PostModel CreateModel(ClaimsPrincipal? user = null)
    {
        var model = new PostModel(_postRepo, _lexical, _mobiledoc, _settings, _gating, _mentions, _sanitizer);
        var httpContext = new DefaultHttpContext();
        if (user is not null) httpContext.User = user;
        model.PageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        return model;
    }

    [Fact]
    public async Task OnGetAsync_PublishedPost_ReturnsPage()
    {
        var post = TestDataFactory.CreatePublishedPost("hello", "Hello");
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        var result = await model.OnGetAsync("hello");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Hello", model.Post.Title);
        Assert.False(model.IsPage);
    }

    [Fact]
    public async Task OnGetAsync_PublishedPage_SetsIsPage()
    {
        var page = TestDataFactory.CreatePublishedPost("about", "About", "page");
        _postRepo.Posts.Add(page);

        var model = CreateModel();
        var result = await model.OnGetAsync("about");

        Assert.IsType<PageResult>(result);
        Assert.True(model.IsPage);
    }

    [Fact]
    public async Task OnGetAsync_DraftPost_ReturnsNotFound()
    {
        var post = TestDataFactory.CreatePublishedPost("draft-post");
        post.Status = "draft";
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        var result = await model.OnGetAsync("draft-post");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NonexistentSlug_ReturnsNotFound()
    {
        var model = CreateModel();
        var result = await model.OnGetAsync("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_RendersHtmlWhenPresent()
    {
        var post = TestDataFactory.CreatePublishedPost("with-html");
        post.Html = "<p>Pre-rendered</p>";
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        await model.OnGetAsync("with-html");

        Assert.Equal("<p>Pre-rendered</p>", model.RenderedContent);
    }

    [Fact]
    public async Task OnGetAsync_RendersLexicalWhenNoHtml()
    {
        var post = TestDataFactory.CreatePublishedPost("lexical");
        post.Html = null;
        post.Lexical = "{\"root\":{}}";
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        await model.OnGetAsync("lexical");

        Assert.Equal("[lexical:{\"root\":{}}]", model.RenderedContent);
    }

    [Fact]
    public async Task OnGetAsync_RendersMobiledocWhenNoHtmlOrLexical()
    {
        var post = TestDataFactory.CreatePublishedPost("mobiledoc");
        post.Html = null;
        post.Lexical = null;
        post.Mobiledoc = "{\"version\":\"0.3.1\"}";
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        await model.OnGetAsync("mobiledoc");

        Assert.Equal("[mobiledoc:{\"version\":\"0.3.1\"}]", model.RenderedContent);
    }

    [Fact]
    public async Task OnGetAsync_GatedContent_EmptyRenderedContent()
    {
        var post = TestDataFactory.CreatePublishedPost("members-only");
        post.Html = "<p>Secret content</p>";
        _postRepo.Posts.Add(post);
        _gating.DefaultLevel = ContentAccessLevel.RequiresMember;

        var model = CreateModel();
        await model.OnGetAsync("members-only");

        Assert.Equal("", model.RenderedContent);
        Assert.True(model.IsContentGated);
    }

    [Fact]
    public async Task OnGetAsync_FullAccess_ShowsContent()
    {
        var post = TestDataFactory.CreatePublishedPost("public-post");
        post.Html = "<p>Public content</p>";
        _postRepo.Posts.Add(post);
        _gating.DefaultLevel = ContentAccessLevel.Full;

        var model = CreateModel();
        await model.OnGetAsync("public-post");

        Assert.Equal("<p>Public content</p>", model.RenderedContent);
        Assert.False(model.IsContentGated);
    }

    [Fact]
    public async Task OnGetAsync_LoadsRelatedPosts_ForPostType()
    {
        var post = TestDataFactory.CreatePublishedPost("main-post");
        var related = TestDataFactory.CreatePublishedPost("related-post");
        _postRepo.Posts.Add(post);
        _postRepo.Posts.Add(related);

        var model = CreateModel();
        await model.OnGetAsync("main-post");

        Assert.NotEmpty(model.RelatedPosts);
    }

    [Fact]
    public async Task OnGetAsync_NoRelatedPosts_ForPageType()
    {
        var page = TestDataFactory.CreatePublishedPost("about", "About", "page");
        _postRepo.Posts.Add(page);

        var model = CreateModel();
        await model.OnGetAsync("about");

        Assert.Empty(model.RelatedPosts);
    }

    [Fact]
    public async Task OnGetAsync_SetsSeoMetadata()
    {
        var post = TestDataFactory.CreatePublishedPost("seo-post", "SEO Post");
        post.Meta = new PostMeta
        {
            Id = "meta1", PostId = post.Id,
            MetaTitle = "Custom SEO Title",
            MetaDescription = "Custom SEO Description",
        };
        _postRepo.Posts.Add(post);
        _settings.Settings["title"] = "MySite";

        var model = CreateModel();
        await model.OnGetAsync("seo-post");

        Assert.Equal("Custom SEO Title", model.ViewData["Title"]);
        Assert.Equal("MySite", model.SiteTitle);
    }

    [Fact]
    public async Task OnGetAsync_PublicTags_FiltersInternal()
    {
        var post = TestDataFactory.CreatePublishedPost("tagged");
        var publicTag = TestDataFactory.CreateTag(name: "Public");
        var internalTag = TestDataFactory.CreateTag(name: "Internal");
        internalTag.Visibility = "internal";
        TestDataFactory.LinkPostToTag(post, publicTag, 0);
        TestDataFactory.LinkPostToTag(post, internalTag, 1);
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        await model.OnGetAsync("tagged");

        Assert.Single(model.PublicTags);
        Assert.Equal("Public", model.PrimaryTag!.Name);
    }

    [Fact]
    public async Task OnGetAsync_Authors_OrderedBySortOrder()
    {
        var post = TestDataFactory.CreatePublishedPost("multi-author");
        var author1 = TestDataFactory.CreateUser(name: "First Author");
        var author2 = TestDataFactory.CreateUser(name: "Second Author");
        TestDataFactory.LinkPostToAuthor(post, author2, 1);
        TestDataFactory.LinkPostToAuthor(post, author1, 0);
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        await model.OnGetAsync("multi-author");

        Assert.Equal(2, model.Authors.Count);
        Assert.Equal("First Author", model.Authors[0].Name);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
