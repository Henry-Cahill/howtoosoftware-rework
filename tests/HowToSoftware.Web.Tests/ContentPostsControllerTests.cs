using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Models.Api;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class ContentPostsControllerTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly ContentPostsController _sut;

    public ContentPostsControllerTests()
    {
        _sut = new ContentPostsController(_postRepo);
    }

    [Fact]
    public async Task GetPosts_ReturnsOkWithEnvelope()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("post-1", "Post 1"));
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("post-2", "Post 2"));

        var result = await _sut.GetPosts();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Equal(2, envelope.Posts.Count);
        Assert.Equal(1, envelope.Meta.Pagination.Page);
        Assert.Equal(2, envelope.Meta.Pagination.Total);
    }

    [Fact]
    public async Task GetPosts_Pagination_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
            _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var result = await _sut.GetPosts(limit: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Equal(2, envelope.Posts.Count);
        Assert.Equal(3, envelope.Meta.Pagination.Pages);
        Assert.Equal(2, envelope.Meta.Pagination.Next);
    }

    [Fact]
    public async Task GetPosts_LimitClamped_Min1()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var result = await _sut.GetPosts(limit: -5);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Equal(1, envelope.Meta.Pagination.Limit);
    }

    [Fact]
    public async Task GetPosts_LimitClamped_Max100()
    {
        var result = await _sut.GetPosts(limit: 500);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Equal(100, envelope.Meta.Pagination.Limit);
    }

    [Fact]
    public async Task GetPosts_FilterByTag()
    {
        var tag = TestDataFactory.CreateTag(slug: "dotnet");
        var post1 = TestDataFactory.CreatePublishedPost("tagged");
        TestDataFactory.LinkPostToTag(post1, tag);
        _postRepo.Posts.Add(post1);
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("untagged"));

        var result = await _sut.GetPosts(filter: "tag:dotnet");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Single(envelope.Posts);
    }

    [Fact]
    public async Task GetPosts_FilterByAuthor()
    {
        var author = TestDataFactory.CreateUser(slug: "john");
        var post1 = TestDataFactory.CreatePublishedPost("by-john");
        TestDataFactory.LinkPostToAuthor(post1, author);
        _postRepo.Posts.Add(post1);
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("by-nobody"));

        var result = await _sut.GetPosts(filter: "author:john");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Single(envelope.Posts);
    }

    [Fact]
    public async Task GetPost_BySlug_ReturnsPost()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("my-slug", "My Post"));

        var result = await _sut.GetPost("my-slug");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Single(envelope.Posts);
    }

    [Fact]
    public async Task GetPost_ById_ReturnsPost()
    {
        var post = TestDataFactory.CreatePublishedPost("test", "Test");
        _postRepo.Posts.Add(post);

        var result = await _sut.GetPost(post.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.Single(envelope.Posts);
    }

    [Fact]
    public async Task GetPost_NotFound_ReturnsNotFound()
    {
        var result = await _sut.GetPost("nonexistent");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<GhostErrorResponse>(notFound.Value);
        Assert.Equal("NotFoundError", error.Errors[0].Type);
    }

    [Fact]
    public async Task GetPosts_WithInclude_PassesThrough()
    {
        var tag = TestDataFactory.CreateTag();
        var post = TestDataFactory.CreatePublishedPost();
        TestDataFactory.LinkPostToTag(post, tag);
        _postRepo.Posts.Add(post);

        var result = await _sut.GetPosts(include: "tags");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostEnvelope<PostResource>>(ok.Value);
        Assert.NotNull(envelope.Posts[0].Tags);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
