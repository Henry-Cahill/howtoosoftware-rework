using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HowToSoftware.Web.Tests;

public class ErrorModelTests
{
    private readonly FakePostRepository _postRepo = new();

    private ErrorModel CreateModel()
    {
        var model = new ErrorModel(_postRepo);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return model;
    }

    [Fact]
    public async Task OnGetAsync_404_LoadsSuggestedPosts()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        await model.OnGetAsync(404);

        Assert.Equal(404, model.ErrorStatusCode);
        Assert.Equal(2, model.SuggestedPosts.Count);
    }

    [Fact]
    public async Task OnGetAsync_500_NoSuggestedPosts()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        await model.OnGetAsync(500);

        Assert.Equal(500, model.ErrorStatusCode);
        Assert.Empty(model.SuggestedPosts);
    }

    [Fact]
    public async Task OnGetAsync_NullStatusCode_Defaults()
    {
        var model = CreateModel();
        await model.OnGetAsync(null);

        // Default HttpContext has 200 status, but the model sets to 500 if 0
        Assert.True(model.ErrorStatusCode > 0);
    }

    [Fact]
    public async Task OnGetAsync_SetsResponseStatusCode()
    {
        var model = CreateModel();
        await model.OnGetAsync(403);

        Assert.Equal(403, model.ErrorStatusCode);
        Assert.Equal(403, model.PageContext.HttpContext.Response.StatusCode);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
