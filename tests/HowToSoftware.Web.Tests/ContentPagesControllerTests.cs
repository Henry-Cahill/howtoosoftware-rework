using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Models.Api;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class ContentPagesControllerTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly ContentPagesController _sut;

    public ContentPagesControllerTests()
    {
        _sut = new ContentPagesController(_postRepo);
    }

    [Fact]
    public async Task GetPages_ReturnsOnlyPages()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("about", "About", "page"));
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("contact", "Contact", "page"));
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("blog-post", "Blog", "post"));

        var result = await _sut.GetPages();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostPagesEnvelope<PostResource>>(ok.Value);
        Assert.Equal(2, envelope.Pages.Count);
    }

    [Fact]
    public async Task GetPages_Pagination_WorksCorrectly()
    {
        for (int i = 0; i < 5; i++)
            _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost(type: "page"));

        var result = await _sut.GetPages(limit: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostPagesEnvelope<PostResource>>(ok.Value);
        Assert.Equal(2, envelope.Pages.Count);
        Assert.Equal(3, envelope.Meta.Pagination.Pages);
    }

    [Fact]
    public async Task GetPage_BySlug_ReturnsPage()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("about", "About", "page"));

        var result = await _sut.GetPage("about");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostPagesEnvelope<PostResource>>(ok.Value);
        Assert.Single(envelope.Pages);
    }

    [Fact]
    public async Task GetPage_PostType_ReturnsNotFound()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("blog-post", "Blog", "post"));

        var result = await _sut.GetPage("blog-post");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPage_NotFound_ReturnsNotFound()
    {
        var result = await _sut.GetPage("nonexistent");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<GhostErrorResponse>(notFound.Value);
        Assert.Equal("NotFoundError", error.Errors[0].Type);
    }

    [Fact]
    public async Task GetPage_WithInclude_PassesThrough()
    {
        var tag = TestDataFactory.CreateTag();
        var page = TestDataFactory.CreatePublishedPost("tagged-page", type: "page");
        TestDataFactory.LinkPostToTag(page, tag);
        _postRepo.Posts.Add(page);

        var result = await _sut.GetPage("tagged-page", include: "tags");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostPagesEnvelope<PostResource>>(ok.Value);
        Assert.NotNull(envelope.Pages[0].Tags);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
