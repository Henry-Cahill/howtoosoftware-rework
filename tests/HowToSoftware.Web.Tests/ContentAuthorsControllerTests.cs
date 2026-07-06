using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Models.Api;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class ContentAuthorsControllerTests
{
    private readonly FakeUserRepository _userRepo = new();
    private readonly ContentAuthorsController _sut;

    public ContentAuthorsControllerTests()
    {
        _sut = new ContentAuthorsController(_userRepo);
    }

    [Fact]
    public async Task GetAuthors_ReturnsOkWithEnvelope()
    {
        _userRepo.Users.Add(TestDataFactory.CreateUser(slug: "john"));
        _userRepo.Users.Add(TestDataFactory.CreateUser(slug: "jane"));

        var result = await _sut.GetAuthors();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.Equal(2, envelope.Authors.Count);
    }

    [Fact]
    public async Task GetAuthors_WithCountPosts_IncludesPostCount()
    {
        _userRepo.Users.Add(TestDataFactory.CreateUser(slug: "author"));

        var result = await _sut.GetAuthors(include: "count.posts");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.NotNull(envelope.Authors[0].Count);
    }

    [Fact]
    public async Task GetAuthors_Pagination_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
            _userRepo.Users.Add(TestDataFactory.CreateUser());

        var result = await _sut.GetAuthors(limit: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.Equal(2, envelope.Authors.Count);
        Assert.Equal(3, envelope.Meta.Pagination.Pages);
    }

    [Fact]
    public async Task GetAuthor_BySlug_ReturnsAuthor()
    {
        _userRepo.Users.Add(TestDataFactory.CreateUser(slug: "john", name: "John"));

        var result = await _sut.GetAuthor("john");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.Single(envelope.Authors);
        Assert.Equal("John", envelope.Authors[0].Name);
    }

    [Fact]
    public async Task GetAuthor_ById_ReturnsAuthor()
    {
        var user = TestDataFactory.CreateUser(id: "user-id-1");
        _userRepo.Users.Add(user);

        var result = await _sut.GetAuthor("user-id-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.Single(envelope.Authors);
    }

    [Fact]
    public async Task GetAuthor_NotFound_ReturnsNotFound()
    {
        var result = await _sut.GetAuthor("nonexistent");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var error = Assert.IsType<GhostErrorResponse>(notFound.Value);
        Assert.Equal("NotFoundError", error.Errors[0].Type);
    }

    [Fact]
    public async Task GetAuthors_ExcludesInactiveAuthors()
    {
        _userRepo.Users.Add(TestDataFactory.CreateUser(slug: "active"));
        var inactive = TestDataFactory.CreateUser(slug: "inactive");
        inactive.Status = "inactive";
        _userRepo.Users.Add(inactive);

        var result = await _sut.GetAuthors();

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<GhostAuthorsEnvelope>(ok.Value);
        Assert.Single(envelope.Authors);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
