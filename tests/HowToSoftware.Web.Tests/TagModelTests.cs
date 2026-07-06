using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HowToSoftware.Web.Tests;

public class TagModelTests
{
    private readonly FakeTagRepository _tagRepo = new();
    private readonly FakePostRepository _postRepo = new();
    private readonly FakeSettingsService _settings = new();

    private TagModel CreateModel()
    {
        var model = new TagModel(_tagRepo, _postRepo, _settings);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        return model;
    }

    [Fact]
    public async Task OnGetAsync_ValidTag_ReturnsPage()
    {
        var tag = TestDataFactory.CreateTag(slug: "csharp", name: "C#");
        _tagRepo.Tags.Add(tag);
        var post = TestDataFactory.CreatePublishedPost();
        TestDataFactory.LinkPostToTag(post, tag);
        _postRepo.Posts.Add(post);

        var model = CreateModel();
        var result = await model.OnGetAsync("csharp");

        Assert.IsType<PageResult>(result);
        Assert.Equal("C#", model.Tag.Name);
        Assert.Single(model.PostCards);
    }

    [Fact]
    public async Task OnGetAsync_NonexistentTag_ReturnsNotFound()
    {
        var model = CreateModel();
        var result = await model.OnGetAsync("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_InternalTag_ReturnsNotFound()
    {
        var tag = TestDataFactory.CreateTag(slug: "internal-tag");
        tag.Visibility = "internal";
        _tagRepo.Tags.Add(tag);

        var model = CreateModel();
        var result = await model.OnGetAsync("internal-tag");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_Pagination_WorksCorrectly()
    {
        var tag = TestDataFactory.CreateTag(slug: "many");
        _tagRepo.Tags.Add(tag);
        for (int i = 0; i < 20; i++)
        {
            var post = TestDataFactory.CreatePublishedPost();
            TestDataFactory.LinkPostToTag(post, tag);
            _postRepo.Posts.Add(post);
        }

        var model = CreateModel();
        await model.OnGetAsync("many", 1);

        Assert.Equal(15, model.PostCards.Count);
        Assert.True(model.HasNextPage);
    }

    [Fact]
    public async Task OnGetAsync_PageBeyondTotal_ReturnsNotFound()
    {
        var tag = TestDataFactory.CreateTag(slug: "small");
        _tagRepo.Tags.Add(tag);
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        var result = await model.OnGetAsync("small", 5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_SetsViewDataTitle()
    {
        var tag = TestDataFactory.CreateTag(slug: "dotnet", name: ".NET");
        _tagRepo.Tags.Add(tag);
        _settings.Settings["title"] = "MySite";

        var model = CreateModel();
        await model.OnGetAsync("dotnet");

        Assert.Equal(".NET - MySite", model.ViewData["Title"]);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
