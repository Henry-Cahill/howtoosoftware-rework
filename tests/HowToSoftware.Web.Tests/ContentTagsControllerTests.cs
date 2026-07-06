using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Models.Api;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class ContentTagsControllerTests
{
    private readonly FakeTagRepository _tagRepo = new();
    private readonly ContentTagsController _sut;

    public ContentTagsControllerTests()
    {
        _sut = new ContentTagsController(_tagRepo);
    }

    [Fact]
    public async Task GetTags_ReturnsOkWithEnvelope()
    {
        _tagRepo.Tags.Add(TestDataFactory.CreateTag(slug: "csharp"));
        _tagRepo.Tags.Add(TestDataFactory.CreateTag(slug: "dotnet"));

        var result = await _sut.GetTags();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Equal(2, envelope.Tags.Count);
    }

    [Fact]
    public async Task GetTags_WithCountPosts_IncludesPostCount()
    {
        var tag = TestDataFactory.CreateTag(slug: "popular");
        _tagRepo.Tags.Add(tag);

        var result = await _sut.GetTags(include: "count.posts");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.NotNull(envelope.Tags[0].Count);
    }

    [Fact]
    public async Task GetTags_WithoutCountPosts_NoCount()
    {
        _tagRepo.Tags.Add(TestDataFactory.CreateTag());

        var result = await _sut.GetTags();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Null(envelope.Tags[0].Count);
    }

    [Fact]
    public async Task GetTags_Pagination_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
            _tagRepo.Tags.Add(TestDataFactory.CreateTag());

        var result = await _sut.GetTags(limit: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Equal(2, envelope.Tags.Count);
        Assert.Equal(3, envelope.Meta.Pagination.Pages);
    }

    [Fact]
    public async Task GetTag_BySlug_ReturnsTag()
    {
        _tagRepo.Tags.Add(TestDataFactory.CreateTag(slug: "csharp", name: "C#"));

        var result = await _sut.GetTag("csharp");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Single(envelope.Tags);
        Assert.Equal("C#", envelope.Tags[0].Name);
    }

    [Fact]
    public async Task GetTag_ById_ReturnsTag()
    {
        var tag = TestDataFactory.CreateTag(id: "tag-id-1", slug: "test");
        _tagRepo.Tags.Add(tag);

        var result = await _sut.GetTag("tag-id-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Single(envelope.Tags);
    }

    [Fact]
    public async Task GetTag_NotFound_ReturnsNotFound()
    {
        var result = await _sut.GetTag("nonexistent");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<GhostErrorResponse>(notFound.Value);
        Assert.Equal("NotFoundError", error.Errors[0].Type);
    }

    [Fact]
    public async Task GetTag_WithCountPosts_IncludesCount()
    {
        _tagRepo.Tags.Add(TestDataFactory.CreateTag(slug: "tag-count"));

        var result = await _sut.GetTag("tag-count", include: "count.posts");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.NotNull(envelope.Tags[0].Count);
    }

    [Fact]
    public async Task GetTags_OnlyReturnsPublicTags()
    {
        _tagRepo.Tags.Add(TestDataFactory.CreateTag(slug: "public-tag"));
        var internalTag = TestDataFactory.CreateTag(slug: "internal-tag");
        internalTag.Visibility = "internal";
        _tagRepo.Tags.Add(internalTag);

        var result = await _sut.GetTags();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostTagsEnvelope>(ok.Value);
        Assert.Single(envelope.Tags);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
