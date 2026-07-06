using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Tests;

public class CollectionServiceTests
{
    private readonly FakeCollectionService _sut = new();

    // ── CreateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ManualCollection_CreatesSuccessfully()
    {
        var result = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Featured Posts",
            Type = "manual",
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Featured Posts", result.Title);
        Assert.Equal("manual", result.Type);
        Assert.Equal("featured-posts", result.Slug);
    }

    [Fact]
    public async Task CreateAsync_AutomaticCollection_SetsFilter()
    {
        var result = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "News",
            Type = "automatic",
            Filter = "tag:news",
        });

        Assert.Equal("automatic", result.Type);
        Assert.Equal("tag:news", result.Filter);
    }

    [Fact]
    public async Task CreateAsync_WithCustomSlug_UsesProvidedSlug()
    {
        var result = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "My Collection",
            Slug = "custom-slug",
            Type = "manual",
        });

        Assert.Equal("custom-slug", result.Slug);
    }

    [Fact]
    public async Task CreateAsync_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var result = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Test",
            Type = "manual",
        });

        Assert.True(result.CreatedAt >= before);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithAllFields_SetsAllProperties()
    {
        var result = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Full Collection",
            Slug = "full",
            Description = "A complete collection",
            Type = "automatic",
            Filter = "tag:tutorials+featured:true",
            FeatureImage = "https://example.com/image.jpg",
        });

        Assert.Equal("A complete collection", result.Description);
        Assert.Equal("tag:tutorials+featured:true", result.Filter);
        Assert.Equal("https://example.com/image.jpg", result.FeatureImage);
    }

    // ── GetAllAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmptyList()
    {
        var results = await _sut.GetAllAsync();
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOrderedByMostRecent()
    {
        await _sut.CreateAsync(new CreateCollectionRequest { Title = "First", Type = "manual" });
        await _sut.CreateAsync(new CreateCollectionRequest { Title = "Second", Type = "manual" });

        var results = await _sut.GetAllAsync();
        Assert.Equal(2, results.Count);
        Assert.Equal("Second", results[0].Title);
        Assert.Equal("First", results[1].Title);
    }

    // ── GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCollection()
    {
        var created = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Test",
            Type = "manual",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync("nonexistent");
        Assert.Null(result);
    }

    // ── GetBySlugAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetBySlugAsync_ValidSlug_ReturnsCollection()
    {
        await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Test Collection",
            Type = "manual",
        });

        var result = await _sut.GetBySlugAsync("test-collection");
        Assert.NotNull(result);
        Assert.Equal("test-collection", result.Slug);
    }

    [Fact]
    public async Task GetBySlugAsync_InvalidSlug_ReturnsNull()
    {
        var result = await _sut.GetBySlugAsync("nonexistent");
        Assert.Null(result);
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesTitle()
    {
        var created = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Old Title",
            Type = "manual",
        });

        await _sut.UpdateAsync(created.Id, new UpdateCollectionRequest
        {
            Title = "New Title",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Equal("New Title", result!.Title);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.UpdateAsync("nonexistent", new UpdateCollectionRequest { Title = "X" }));
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAt()
    {
        var created = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Test",
            Type = "manual",
        });

        var before = DateTime.UtcNow;
        await _sut.UpdateAsync(created.Id, new UpdateCollectionRequest
        {
            Description = "Updated",
        });

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.NotNull(result!.UpdatedAt);
        Assert.True(result.UpdatedAt >= before);
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesCollection()
    {
        var created = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "To Delete",
            Type = "manual",
        });

        await _sut.DeleteAsync(created.Id);

        var result = await _sut.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.DeleteAsync("nonexistent"));
    }

    // ── Manual collection posts ─────────────────────────────────

    [Fact]
    public async Task AddPostAsync_ManualCollection_AddsPost()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Manual",
            Type = "manual",
        });

        await _sut.AddPostAsync(col.Id, "post-1");

        var posts = await _sut.GetPostsAsync(col.Id);
        Assert.Single(posts);
        Assert.Equal("post-1", posts[0].Id);
    }

    [Fact]
    public async Task AddPostAsync_DuplicatePost_DoesNotAddTwice()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Manual",
            Type = "manual",
        });

        await _sut.AddPostAsync(col.Id, "post-1");
        await _sut.AddPostAsync(col.Id, "post-1");

        var posts = await _sut.GetPostsAsync(col.Id);
        Assert.Single(posts);
    }

    [Fact]
    public async Task AddPostAsync_AutomaticCollection_Throws()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Auto",
            Type = "automatic",
            Filter = "tag:news",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddPostAsync(col.Id, "post-1"));
    }

    [Fact]
    public async Task RemovePostAsync_RemovesPost()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Manual",
            Type = "manual",
        });

        await _sut.AddPostAsync(col.Id, "post-1");
        await _sut.AddPostAsync(col.Id, "post-2");
        await _sut.RemovePostAsync(col.Id, "post-1");

        var posts = await _sut.GetPostsAsync(col.Id);
        Assert.Single(posts);
        Assert.Equal("post-2", posts[0].Id);
    }

    [Fact]
    public async Task RemovePostAsync_AutomaticCollection_Throws()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Auto",
            Type = "automatic",
            Filter = "tag:news",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RemovePostAsync(col.Id, "post-1"));
    }

    // ── ReorderPostsAsync ───────────────────────────────────────

    [Fact]
    public async Task ReorderPostsAsync_ManualCollection_ReordersPosts()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Manual",
            Type = "manual",
        });

        await _sut.AddPostAsync(col.Id, "post-1");
        await _sut.AddPostAsync(col.Id, "post-2");
        await _sut.AddPostAsync(col.Id, "post-3");

        await _sut.ReorderPostsAsync(col.Id, ["post-3", "post-1", "post-2"]);

        var posts = await _sut.GetPostsAsync(col.Id);
        Assert.Equal("post-3", posts[0].Id);
        Assert.Equal("post-1", posts[1].Id);
        Assert.Equal("post-2", posts[2].Id);
    }

    [Fact]
    public async Task ReorderPostsAsync_AutomaticCollection_Throws()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Auto",
            Type = "automatic",
            Filter = "tag:news",
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ReorderPostsAsync(col.Id, ["post-1"]));
    }

    // ── GetPostCountAsync ───────────────────────────────────────

    [Fact]
    public async Task GetPostCountAsync_ReturnsCorrectCount()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Manual",
            Type = "manual",
        });

        Assert.Equal(0, await _sut.GetPostCountAsync(col.Id));

        await _sut.AddPostAsync(col.Id, "post-1");
        await _sut.AddPostAsync(col.Id, "post-2");

        Assert.Equal(2, await _sut.GetPostCountAsync(col.Id));
    }

    [Fact]
    public async Task GetPostCountAsync_InvalidId_ReturnsZero()
    {
        Assert.Equal(0, await _sut.GetPostCountAsync("nonexistent"));
    }

    // ── GetPostsAsync (automatic) ───────────────────────────────

    [Fact]
    public async Task GetPostsAsync_AutomaticCollection_ReturnsFilteredPosts()
    {
        var col = await _sut.CreateAsync(new CreateCollectionRequest
        {
            Title = "Featured",
            Type = "automatic",
            Filter = "featured:true",
        });

        var posts = await _sut.GetPostsAsync(col.Id);
        // The fake returns all seeded posts matching the filter
        Assert.All(posts, p => Assert.True(p.Featured));
    }

    [Fact]
    public async Task PreviewPostsAsync_LimitsResults()
    {
        var posts = await _sut.PreviewPostsAsync(null, 2);

        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public async Task PreviewPostsAsync_AppliesFilter()
    {
        var posts = await _sut.PreviewPostsAsync("featured:true", 10);

        Assert.NotEmpty(posts);
        Assert.All(posts, p => Assert.True(p.Featured));
    }

    // ================================================================
    // Fake in-memory implementation
    // ================================================================

    private sealed class FakeCollectionService : ICollectionService
    {
        private readonly List<Collection> _collections = [];
        private readonly List<CollectionsPost> _links = [];
        private readonly List<Post> _posts =
        [
            new() { Id = "post-1", Title = "Post One", Slug = "post-one", Status = "published", Type = "post", Featured = true, CreatedAt = DateTime.UtcNow },
            new() { Id = "post-2", Title = "Post Two", Slug = "post-two", Status = "published", Type = "post", Featured = false, CreatedAt = DateTime.UtcNow },
            new() { Id = "post-3", Title = "Post Three", Slug = "post-three", Status = "published", Type = "post", Featured = true, CreatedAt = DateTime.UtcNow },
        ];

        public Task<List<Collection>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_collections
                .OrderByDescending(c => c.CreatedAt)
                .ToList());
        }

        public Task<Collection?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_collections.FirstOrDefault(c => c.Id == id));
        }

        public Task<Collection?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            return Task.FromResult(_collections.FirstOrDefault(c => c.Slug == slug));
        }

        public Task<Collection> CreateAsync(
            CreateCollectionRequest request, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var slug = !string.IsNullOrWhiteSpace(request.Slug)
                ? request.Slug
                : GenerateSlug(request.Title);

            var collection = new Collection
            {
                Id = Guid.NewGuid().ToString("D"),
                Title = request.Title,
                Slug = slug,
                Description = request.Description,
                Type = request.Type,
                Filter = request.Filter,
                FeatureImage = request.FeatureImage,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _collections.Add(collection);
            return Task.FromResult(collection);
        }

        public Task UpdateAsync(
            string id, UpdateCollectionRequest request, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == id)
                ?? throw new InvalidOperationException($"Collection {id} not found");

            if (!string.IsNullOrWhiteSpace(request.Title))
                col.Title = request.Title;
            if (request.Slug is not null)
                col.Slug = request.Slug;
            if (request.Description is not null)
                col.Description = request.Description;
            if (request.Filter is not null)
                col.Filter = request.Filter;
            if (request.FeatureImage is not null)
                col.FeatureImage = request.FeatureImage;

            col.UpdatedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == id)
                ?? throw new InvalidOperationException($"Collection {id} not found");

            _collections.Remove(col);
            _links.RemoveAll(l => l.CollectionId == id);
            return Task.CompletedTask;
        }

        public Task<List<Post>> GetPostsAsync(string collectionId, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == collectionId)
                ?? throw new InvalidOperationException($"Collection {collectionId} not found");

            if (col.Type == "automatic")
            {
                return PreviewPostsAsync(col.Filter, int.MaxValue, ct);
            }

            // Manual: return via join table ordered by SortOrder
            var postIds = _links
                .Where(l => l.CollectionId == collectionId)
                .OrderBy(l => l.SortOrder)
                .Select(l => l.PostId)
                .ToList();

            var posts = postIds
                .Select(pid => _posts.FirstOrDefault(p => p.Id == pid))
                .Where(p => p is not null)
                .Cast<Post>()
                .ToList();

            return Task.FromResult(posts);
        }

        public Task<List<Post>> PreviewPostsAsync(string? filter, int limit = 10, CancellationToken ct = default)
        {
            if (limit < 1)
                return Task.FromResult(new List<Post>());

            var filtered = ApplyFilter(_posts, filter)
                .Where(p => p.Status == "published" && p.Type == "post")
                .Take(limit)
                .ToList();

            return Task.FromResult(filtered);
        }

        public Task AddPostAsync(string collectionId, string postId, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == collectionId)
                ?? throw new InvalidOperationException($"Collection {collectionId} not found");

            if (col.Type == "automatic")
                throw new InvalidOperationException("Cannot manually add posts to an automatic collection");

            if (_links.Any(l => l.CollectionId == collectionId && l.PostId == postId))
                return Task.CompletedTask;

            var maxSort = _links
                .Where(l => l.CollectionId == collectionId)
                .Select(l => (int?)l.SortOrder)
                .Max() ?? -1;

            _links.Add(new CollectionsPost
            {
                Id = Guid.NewGuid().ToString("D"),
                CollectionId = collectionId,
                PostId = postId,
                SortOrder = maxSort + 1,
            });

            return Task.CompletedTask;
        }

        public Task RemovePostAsync(string collectionId, string postId, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == collectionId)
                ?? throw new InvalidOperationException($"Collection {collectionId} not found");

            if (col.Type == "automatic")
                throw new InvalidOperationException("Cannot manually remove posts from an automatic collection");

            _links.RemoveAll(l => l.CollectionId == collectionId && l.PostId == postId);
            return Task.CompletedTask;
        }

        public Task ReorderPostsAsync(
            string collectionId, List<string> orderedPostIds, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == collectionId)
                ?? throw new InvalidOperationException($"Collection {collectionId} not found");

            if (col.Type == "automatic")
                throw new InvalidOperationException("Cannot reorder posts in an automatic collection");

            var links = _links.Where(l => l.CollectionId == collectionId).ToList();
            var linksByPostId = links.ToDictionary(l => l.PostId);

            for (int i = 0; i < orderedPostIds.Count; i++)
            {
                if (linksByPostId.TryGetValue(orderedPostIds[i], out var link))
                    link.SortOrder = i;
            }

            return Task.CompletedTask;
        }

        public Task<int> GetPostCountAsync(string collectionId, CancellationToken ct = default)
        {
            var col = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (col is null) return Task.FromResult(0);

            if (col.Type == "automatic")
            {
                var count = _posts.Count(p => p.Status == "published" && p.Type == "post");
                return Task.FromResult(count);
            }

            return Task.FromResult(_links.Count(l => l.CollectionId == collectionId));
        }

        private static string GenerateSlug(string text)
        {
            return text.ToLowerInvariant()
                .Replace(' ', '-')
                .Replace("--", "-")
                .Trim('-');
        }

        private static IEnumerable<Post> ApplyFilter(IEnumerable<Post> posts, string? filter)
        {
            var filtered = posts;

            if (string.IsNullOrWhiteSpace(filter))
                return filtered;

            foreach (var segment in filter.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var part = segment;
                var negated = part.StartsWith('-');
                if (negated)
                    part = part[1..];

                var colonIdx = part.IndexOf(':');
                if (colonIdx <= 0)
                    continue;

                var key = part[..colonIdx].ToLowerInvariant();
                var value = part[(colonIdx + 1)..];

                filtered = (key, negated) switch
                {
                    ("featured", false) => bool.TryParse(value, out var featured)
                        ? filtered.Where(p => p.Featured == featured)
                        : filtered,
                    ("featured", true) => bool.TryParse(value, out var notFeatured)
                        ? filtered.Where(p => p.Featured != notFeatured)
                        : filtered,
                    _ => filtered,
                };
            }

            return filtered;
        }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
