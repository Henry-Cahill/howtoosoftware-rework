using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class SearchServiceTests
{
    private readonly FakeSearchRepository _repo = new();
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _sut = new SearchService(_repo);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyResult()
    {
        var result = await _sut.SearchAsync(new SearchRequest { Query = "" });

        Assert.Empty(result.Posts);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmptyResult()
    {
        var result = await _sut.SearchAsync(new SearchRequest { Query = "   " });

        Assert.Empty(result.Posts);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ForwardsToRepository()
    {
        _repo.SeedPosts(
            CreatePost("1", "Getting Started with C#", "Learn C# basics"),
            CreatePost("2", "Advanced SQL Server", "SQL Server tips")
        );

        var result = await _sut.SearchAsync(new SearchRequest { Query = "C#" });

        Assert.Single(result.Posts);
        Assert.Equal("Getting Started with C#", result.Posts[0].Title);
    }

    [Fact]
    public async Task SearchAsync_ReturnsMultipleMatches()
    {
        _repo.SeedPosts(
            CreatePost("1", "Docker Basics", "docker containers"),
            CreatePost("2", "Docker Compose", "docker compose tutorial"),
            CreatePost("3", "ASP.NET Core", "web framework")
        );

        var result = await _sut.SearchAsync(new SearchRequest { Query = "Docker" });

        Assert.Equal(2, result.Posts.Count);
    }

    [Fact]
    public async Task SearchAsync_FilterByType_Post()
    {
        _repo.SeedPosts(
            CreatePost("1", "Blog Post", "content", type: "post"),
            CreatePost("2", "About Page", "about us", type: "page")
        );

        var result = await _sut.SearchAsync(new SearchRequest { Query = "content", Type = "post" });

        Assert.Single(result.Posts);
        Assert.Equal("Blog Post", result.Posts[0].Title);
    }

    [Fact]
    public async Task SearchAsync_FilterByType_Page()
    {
        _repo.SeedPosts(
            CreatePost("1", "Blog Post", "content", type: "post"),
            CreatePost("2", "About Page", "content", type: "page")
        );

        var result = await _sut.SearchAsync(new SearchRequest { Query = "content", Type = "page" });

        Assert.Single(result.Posts);
        Assert.Equal("About Page", result.Posts[0].Title);
    }

    [Fact]
    public async Task SearchAsync_Pagination_RespectsPageAndSize()
    {
        _repo.SeedPosts(
            CreatePost("1", "Post One", "search term"),
            CreatePost("2", "Post Two", "search term"),
            CreatePost("3", "Post Three", "search term")
        );

        var result = await _sut.SearchAsync(new SearchRequest
        {
            Query = "search term",
            Page = 2,
            PageSize = 2
        });

        Assert.Single(result.Posts);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task SearchAsync_ClampsPageSizeToMax100()
    {
        _repo.SeedPosts(CreatePost("1", "Test", "test content"));

        var result = await _sut.SearchAsync(new SearchRequest
        {
            Query = "test",
            PageSize = 200
        });

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task SearchAsync_NegativePage_ClampsTo1()
    {
        _repo.SeedPosts(CreatePost("1", "Test", "test content"));

        var result = await _sut.SearchAsync(new SearchRequest
        {
            Query = "test",
            Page = -5
        });

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task SearchAsync_NoMatches_ReturnsEmptyWithZeroTotal()
    {
        _repo.SeedPosts(CreatePost("1", "C# Guide", "learn C#"));

        var result = await _sut.SearchAsync(new SearchRequest { Query = "Python" });

        Assert.Empty(result.Posts);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_TrimsQuery()
    {
        _repo.SeedPosts(CreatePost("1", "Trimming Test", "trim content"));

        var result = await _sut.SearchAsync(new SearchRequest { Query = "  Trimming  " });

        Assert.Single(result.Posts);
    }

    private static Post CreatePost(string id, string title, string plaintext, string type = "post")
    {
        return new Post
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            Title = title,
            Slug = title.ToLowerInvariant().Replace(' ', '-'),
            Plaintext = plaintext,
            Type = type,
            Status = "published",
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// In-memory fake that mimics LIKE-based search on title and plaintext.
    /// </summary>
    private sealed class FakeSearchRepository : ISearchRepository
    {
        private List<Post> _posts = [];

        public void SeedPosts(params Post[] posts) => _posts = [.. posts];

        public Task<PagedResult<Post>> SearchPostsAsync(
            string query, string? type, int page, int pageSize, CancellationToken ct = default)
        {
            var q = _posts
                .Where(p => p.Status == "published")
                .Where(p =>
                    p.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (p.Plaintext?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    || (p.CustomExcerpt?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));

            if (!string.IsNullOrEmpty(type))
                q = q.Where(p => p.Type == type);

            var all = q.OrderByDescending(p => p.PublishedAt).ToList();
            var totalCount = all.Count;
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult(new PagedResult<Post>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
