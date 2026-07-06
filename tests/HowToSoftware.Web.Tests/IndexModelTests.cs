using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Pages;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace HowToSoftware.Web.Tests;

public class IndexModelTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly FakeSettingsService _settings = new();

    private IndexModel CreateModel()
    {
        var model = new IndexModel(_postRepo, _settings);
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        return model;
    }

    [Fact]
    public async Task OnGetAsync_ReturnsPage_WithPostCards()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("hello-world", "Hello World"));
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost("second-post", "Second Post"));
        _settings.Settings["title"] = "My Site";
        _settings.Settings["description"] = "A test site";

        var model = CreateModel();
        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(2, model.PostCards.Count);
        Assert.Equal("My Site", model.SiteTitle);
        Assert.Equal("A test site", model.SiteDescription);
    }

    [Fact]
    public async Task OnGetAsync_DefaultTitleWhenNoSetting()
    {
        var model = CreateModel();
        await model.OnGetAsync();

        Assert.Equal("howtosoftware", model.SiteTitle);
    }

    [Fact]
    public async Task OnGetAsync_Page1_ShowsPagination()
    {
        for (int i = 0; i < 20; i++)
            _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        await model.OnGetAsync(1);

        Assert.Equal(1, model.CurrentPage);
        Assert.Equal(15, model.PostCards.Count);
        Assert.True(model.HasNextPage);
        Assert.False(model.HasPreviousPage);
    }

    [Fact]
    public async Task OnGetAsync_Page2_ShowsPreviousPage()
    {
        for (int i = 0; i < 20; i++)
            _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        await model.OnGetAsync(2);

        Assert.Equal(2, model.CurrentPage);
        Assert.True(model.HasPreviousPage);
        Assert.False(model.HasNextPage);
    }

    [Fact]
    public async Task OnGetAsync_InvalidPageBeyondTotal_ReturnsNotFound()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        var result = await model.OnGetAsync(5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_NegativePage_ClampedTo1()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());

        var model = CreateModel();
        var result = await model.OnGetAsync(-1);

        Assert.IsType<PageResult>(result);
        Assert.Equal(1, model.CurrentPage);
    }

    [Fact]
    public async Task OnGetAsync_OnlyIncludesPublishedPosts()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost());
        var draft = TestDataFactory.CreatePublishedPost();
        draft.Status = "draft";
        _postRepo.Posts.Add(draft);

        var model = CreateModel();
        await model.OnGetAsync();

        Assert.Single(model.PostCards);
    }

    [Fact]
    public async Task OnGetAsync_ExcludesPages()
    {
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost(type: "post"));
        _postRepo.Posts.Add(TestDataFactory.CreatePublishedPost(type: "page"));

        var model = CreateModel();
        await model.OnGetAsync();

        Assert.Single(model.PostCards);
    }

    [Fact]
    public void EstimateReadingTime_ShortPost_Returns1()
    {
        var post = new Post { Plaintext = "Short" };
        Assert.Equal(1, IndexModel.EstimateReadingTime(post));
    }

    [Fact]
    public void EstimateReadingTime_LongPost_CalculatesCorrectly()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 550));
        var post = new Post { Plaintext = words };
        Assert.Equal(2, IndexModel.EstimateReadingTime(post));
    }

    [Fact]
    public void EstimateReadingTime_NullPlaintext_Returns1()
    {
        var post = new Post { Plaintext = null };
        Assert.Equal(1, IndexModel.EstimateReadingTime(post));
    }

    [Fact]
    public void GetExcerpt_ShortText_ReturnsAsIs()
    {
        var post = new Post { CustomExcerpt = "Short excerpt" };
        Assert.Equal("Short excerpt", IndexModel.GetExcerpt(post));
    }

    [Fact]
    public void GetExcerpt_LongText_Truncates()
    {
        var post = new Post { Plaintext = new string('a', 200) };
        var excerpt = IndexModel.GetExcerpt(post, 150);
        Assert.NotNull(excerpt);
        Assert.True(excerpt.Length <= 151); // 150 + ellipsis
        Assert.EndsWith("\u2026", excerpt);
    }

    [Fact]
    public void GetExcerpt_PrefersCustomExcerpt()
    {
        var post = new Post { CustomExcerpt = "Custom", Plaintext = "Plaintext" };
        Assert.Equal("Custom", IndexModel.GetExcerpt(post));
    }

    [Fact]
    public void GetExcerpt_NullText_ReturnsNull()
    {
        var post = new Post { CustomExcerpt = null, Plaintext = null };
        Assert.Null(IndexModel.GetExcerpt(post));
    }

    [Fact]
    public void ToPostCard_MapsFieldsCorrectly()
    {
        var tag = TestDataFactory.CreateTag(slug: "csharp", name: "C#");
        var post = TestDataFactory.CreatePublishedPost("my-post", "My Post");
        post.FeatureImage = "/images/test.jpg";
        post.Featured = true;
        TestDataFactory.LinkPostToTag(post, tag);

        var card = IndexModel.ToPostCard(post);

        Assert.Equal("my-post", card.Slug);
        Assert.Equal("My Post", card.Title);
        Assert.Equal("/images/test.jpg", card.FeatureImage);
        Assert.True(card.Featured);
        Assert.Equal("C#", card.PrimaryTagName);
        Assert.Contains("featured", card.CssClasses);
    }

    [Fact]
    public void ToPostCard_NoPrimaryTag_WhenInternalOnly()
    {
        var tag = TestDataFactory.CreateTag();
        tag.Visibility = "internal";
        var post = TestDataFactory.CreatePublishedPost();
        TestDataFactory.LinkPostToTag(post, tag);

        var card = IndexModel.ToPostCard(post);

        Assert.Null(card.PrimaryTagName);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
