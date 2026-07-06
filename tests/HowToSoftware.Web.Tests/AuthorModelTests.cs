using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HowToSoftware.Web.Tests;

public class AuthorModelTests
{
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakePostRepository _postRepo = new();
    private readonly FakeSettingsService _settings = new();

    private AuthorModel CreateModel()
    {
        var model = new AuthorModel(_userRepo, _postRepo, _settings);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        return model;
    }

    [Fact]
    public async Task OnGetAsync_ValidAuthor_ReturnsPage()
    {
        var author = TestDataFactory.CreateUser(slug: "john", name: "John Doe");
        _userRepo.Users.Add(author);
        var post = TestDataFactory.CreatePublishedPost();
        TestDataFactory.LinkPostToAuthor(post, author);
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        var result = await model.OnGetAsync("john");

        Assert.IsType<PageResult>(result);
        Assert.Equal("John Doe", model.Author.Name);
        Assert.Single(model.PostCards);
    }

    [Fact]
    public async Task OnGetAsync_NonexistentAuthor_ReturnsNotFound()
    {
        var model = CreateModel();
        var result = await model.OnGetAsync("nobody");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_InactiveAuthor_ReturnsNotFound()
    {
        var author = TestDataFactory.CreateUser(slug: "inactive");
        author.Status = "inactive";
        _userRepo.Users.Add(author);

        var model = CreateModel();
        var result = await model.OnGetAsync("inactive");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_SetsViewDataTitle()
    {
        var author = TestDataFactory.CreateUser(slug: "jane", name: "Jane");
        _userRepo.Users.Add(author);
        _settings.Settings["title"] = "MySite";

        var model = CreateModel();
        await model.OnGetAsync("jane");

        Assert.Equal("Jane - MySite", model.ViewData["Title"]);
    }

    [Fact]
    public async Task OnGetAsync_PageBeyondTotal_ReturnsNotFound()
    {
        var author = TestDataFactory.CreateUser(slug: "solo");
        _userRepo.Users.Add(author);

        var model = CreateModel();
        var result = await model.OnGetAsync("solo", 5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_Pagination_WorksCorrectly()
    {
        var author = TestDataFactory.CreateUser(slug: "prolific");
        _userRepo.Users.Add(author);
        for (int i = 0; i < 20; i++)
        {
            var post = TestDataFactory.CreatePublishedPost();
            TestDataFactory.LinkPostToAuthor(post, author);
            _postRepo.Posts.Add(post);
        }

        var model = CreateModel();
        await model.OnGetAsync("prolific", 1);

        Assert.Equal(15, model.PostCards.Count);
        Assert.True(model.HasNextPage);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
