using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class ContentServiceTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly SlugGenerator _slugGenerator = new();
    private readonly FakeLexicalRenderer _lexicalRenderer = new();
    private readonly FakeMobiledocRenderer _mobiledocRenderer = new();
    private readonly FakeWebhookDispatch _webhookDispatch = new();
    private readonly FakeIndexNowService _indexNow = new();
    private readonly ContentService _sut;

    private const string AuthorId = "aaaaaaaaaaaaaaaaaaaaaaaa";

    public ContentServiceTests()
    {
        _sut = new ContentService(_postRepo, _slugGenerator, _lexicalRenderer, _mobiledocRenderer, _webhookDispatch, _indexNow);
    }

    // ── CreateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_BasicPost_SetsAllFields()
    {
        var request = new ContentCreateRequest { Title = "Hello World" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.NotNull(post.Id);
        Assert.Equal(24, post.Id.Length);
        Assert.Equal("hello-world", post.Slug);
        Assert.Equal("Hello World", post.Title);
        Assert.Equal("post", post.Type);
        Assert.Equal("draft", post.Status);
        Assert.Equal("public", post.Visibility);
        Assert.True(post.ShowTitleAndFeatureImage);
        Assert.NotNull(post.Uuid);
        Assert.NotEqual(DateTime.MinValue, post.CreatedAt);
        Assert.Single(_postRepo.Posts);
    }

    [Fact]
    public async Task CreateAsync_Page_SetsTypePage()
    {
        var request = new ContentCreateRequest { Title = "About", Type = "page" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal("page", post.Type);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_DefaultsToPost()
    {
        var request = new ContentCreateRequest { Title = "Test", Type = "invalid" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal("post", post.Type);
    }

    [Fact]
    public async Task CreateAsync_WithLexical_RendersHtml()
    {
        var request = new ContentCreateRequest
        {
            Title = "Test",
            Lexical = "{\"root\":{}}",
        };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal("[lexical:{\"root\":{}}]", post.Html);
        Assert.NotNull(post.Plaintext);
    }

    [Fact]
    public async Task CreateAsync_WithMobiledoc_RendersHtml()
    {
        var request = new ContentCreateRequest
        {
            Title = "Test",
            Mobiledoc = "{\"version\":\"0.3.1\"}",
        };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal("[mobiledoc:{\"version\":\"0.3.1\"}]", post.Html);
    }

    [Fact]
    public async Task CreateAsync_WithCustomSlug_UsesIt()
    {
        var request = new ContentCreateRequest { Title = "Test", Slug = "custom-slug" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal("custom-slug", post.Slug);
    }

    [Fact]
    public async Task CreateAsync_SetsAuthor()
    {
        var request = new ContentCreateRequest { Title = "Test" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Single(post.PostsAuthors);
        Assert.Equal(AuthorId, post.PostsAuthors.First().AuthorId);
        Assert.Equal(0, post.PostsAuthors.First().SortOrder);
    }

    [Fact]
    public async Task CreateAsync_WithTags_AddsSorted()
    {
        var request = new ContentCreateRequest
        {
            Title = "Test",
            TagIds = ["tag1", "tag2", "tag3"],
        };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Equal(3, post.PostsTags.Count);
        var tags = post.PostsTags.OrderBy(t => t.SortOrder).ToList();
        Assert.Equal("tag1", tags[0].TagId);
        Assert.Equal("tag2", tags[1].TagId);
        Assert.Equal("tag3", tags[2].TagId);
    }

    [Fact]
    public async Task CreateAsync_WithMeta_CreatesPostMeta()
    {
        var request = new ContentCreateRequest
        {
            Title = "Test",
            Meta = new PostMetaRequest
            {
                MetaTitle = "SEO Title",
                MetaDescription = "SEO description",
                OgTitle = "OG Title",
            },
        };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.NotNull(post.Meta);
        Assert.Equal("SEO Title", post.Meta.MetaTitle);
        Assert.Equal("SEO description", post.Meta.MetaDescription);
        Assert.Equal("OG Title", post.Meta.OgTitle);
    }

    [Fact]
    public async Task CreateAsync_CreatesInitialRevision()
    {
        var request = new ContentCreateRequest { Title = "Test" };

        var post = await _sut.CreateAsync(request, AuthorId);

        Assert.Single(post.Revisions);
        var rev = post.Revisions.First();
        Assert.Equal("initial", rev.Reason);
        Assert.Equal(AuthorId, rev.AuthorId);
        Assert.Equal("Test", rev.Title);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_GeneratesUnique()
    {
        // Create first post
        await _sut.CreateAsync(new ContentCreateRequest { Title = "Hello" }, AuthorId);

        // Create second with same title
        var post2 = await _sut.CreateAsync(new ContentCreateRequest { Title = "Hello" }, AuthorId);

        Assert.Equal("hello-2", post2.Slug);
    }

    // ── UpdateAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Title_UpdatesTitleAndCreatesRevision()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Old" }, AuthorId);
        var editorId = "bbbbbbbbbbbbbbbbbbbbbbbb";

        var updated = await _sut.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "New" }, editorId);

        Assert.Equal("New", updated.Title);
        // Initial + edited revision
        Assert.Equal(2, updated.Revisions.Count);
        Assert.Contains(updated.Revisions, r => r.Reason == "edited");
    }

    [Fact]
    public async Task UpdateAsync_Lexical_ReRendersHtml()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", Lexical = "{\"old\":true}" },
            AuthorId);

        var updated = await _sut.UpdateAsync(
            post.Id,
            new ContentUpdateRequest { Lexical = "{\"new\":true}" },
            AuthorId);

        Assert.Equal("[lexical:{\"new\":true}]", updated.Html);
        Assert.Null(updated.Mobiledoc); // Lexical takes precedence
    }

    [Fact]
    public async Task UpdateAsync_Mobiledoc_ClearsLexical()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", Lexical = "{\"old\":true}" },
            AuthorId);

        var updated = await _sut.UpdateAsync(
            post.Id,
            new ContentUpdateRequest { Mobiledoc = "{\"version\":\"0.3.1\"}" },
            AuthorId);

        Assert.Null(updated.Lexical);
        Assert.Equal("[mobiledoc:{\"version\":\"0.3.1\"}]", updated.Html);
    }

    [Fact]
    public async Task UpdateAsync_Tags_ReplacesAll()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", TagIds = ["old-tag"] },
            AuthorId);

        var updated = await _sut.UpdateAsync(
            post.Id,
            new ContentUpdateRequest { TagIds = ["new-tag-1", "new-tag-2"] },
            AuthorId);

        Assert.Equal(2, updated.PostsTags.Count);
        Assert.DoesNotContain(updated.PostsTags, t => t.TagId == "old-tag");
    }

    [Fact]
    public async Task UpdateAsync_NoContentChange_NoRevisionAdded()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        var updated = await _sut.UpdateAsync(
            post.Id,
            new ContentUpdateRequest { Featured = true },
            AuthorId);

        Assert.True(updated.Featured);
        // Only initial revision — no "edited" revision
        Assert.Single(updated.Revisions);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync("nonexistent", new ContentUpdateRequest { Title = "X" }, AuthorId));
    }

    // ── PublishAsync ────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_DraftPost_BecomesPublished()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        var published = await _sut.PublishAsync(post.Id, AuthorId);

        Assert.Equal("published", published.Status);
        Assert.NotNull(published.PublishedAt);
        Assert.Equal(AuthorId, published.PublishedBy);
    }

    [Fact]
    public async Task PublishAsync_AlreadyPublished_NoOp()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);
        await _sut.PublishAsync(post.Id, AuthorId);
        var revCountAfterPublish = post.Revisions.Count;

        var again = await _sut.PublishAsync(post.Id, AuthorId);

        Assert.Equal("published", again.Status);
        Assert.Equal(revCountAfterPublish, again.Revisions.Count);
    }

    [Fact]
    public async Task PublishAsync_SetsPublishedAtOnce()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);
        var published = await _sut.PublishAsync(post.Id, AuthorId);
        var firstPublishedAt = published.PublishedAt;

        // Unpublish then re-publish — should keep original PublishedAt
        await _sut.UnpublishAsync(post.Id);
        var republished = await _sut.PublishAsync(post.Id, AuthorId);

        Assert.Equal(firstPublishedAt, republished.PublishedAt);
    }

    [Fact]
    public async Task PublishAsync_CreatesRevision()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        await _sut.PublishAsync(post.Id, AuthorId);

        Assert.Contains(post.Revisions, r => r.Reason == "published");
    }

    // ── UnpublishAsync ──────────────────────────────────────────

    [Fact]
    public async Task UnpublishAsync_PublishedPost_RevertsToDraft()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);
        await _sut.PublishAsync(post.Id, AuthorId);

        var unpublished = await _sut.UnpublishAsync(post.Id);

        Assert.Equal("draft", unpublished.Status);
    }

    [Fact]
    public async Task UnpublishAsync_AlreadyDraft_NoOp()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        var result = await _sut.UnpublishAsync(post.Id);

        Assert.Equal("draft", result.Status);
    }

    // ── ScheduleAsync ───────────────────────────────────────────

    [Fact]
    public async Task ScheduleAsync_FutureDate_SetsScheduledStatus()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);
        var futureDate = DateTime.UtcNow.AddDays(7);

        var scheduled = await _sut.ScheduleAsync(post.Id, futureDate, AuthorId);

        Assert.Equal("scheduled", scheduled.Status);
        Assert.Equal(futureDate, scheduled.PublishedAt);
        Assert.Equal(AuthorId, scheduled.PublishedBy);
    }

    [Fact]
    public async Task ScheduleAsync_PastDate_Throws()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ScheduleAsync(post.Id, DateTime.UtcNow.AddMinutes(-5), AuthorId));
    }

    [Fact]
    public async Task ScheduleAsync_CreatesRevision()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        await _sut.ScheduleAsync(post.Id, DateTime.UtcNow.AddDays(1), AuthorId);

        Assert.Contains(post.Revisions, r => r.Reason == "scheduled");
    }

    // ── SendAsEmailAsync ────────────────────────────────────────

    [Fact]
    public async Task SendAsEmailAsync_ValidPost_CreatesEmail()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Newsletter Post", Lexical = "{}" },
            AuthorId);
        var newsletterId = "cccccccccccccccccccccccc";

        var email = await _sut.SendAsEmailAsync(post.Id, newsletterId, "all");

        Assert.NotNull(email);
        Assert.Equal(24, email.Id.Length);
        Assert.Equal(post.Id, email.PostId);
        Assert.Equal("pending", email.Status);
        Assert.Equal(newsletterId, email.NewsletterId);
        Assert.Equal("Newsletter Post", email.Subject);
        Assert.True(email.TrackOpens);
        Assert.True(email.TrackClicks);
    }

    [Fact]
    public async Task SendAsEmailAsync_SetsNewsletterOnPost()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", Lexical = "{}" },
            AuthorId);
        var newsletterId = "cccccccccccccccccccccccc";

        await _sut.SendAsEmailAsync(post.Id, newsletterId, "status:free");

        var updated = _postRepo.Posts.First(p => p.Id == post.Id);
        Assert.Equal(newsletterId, updated.NewsletterId);
        Assert.Equal("status:free", updated.EmailRecipientFilter);
    }

    [Fact]
    public async Task SendAsEmailAsync_PostWithCustomSubject_UsesIt()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest
            {
                Title = "Test",
                Lexical = "{}",
                Meta = new PostMetaRequest { EmailSubject = "Custom Subject Line" },
            },
            AuthorId);

        var email = await _sut.SendAsEmailAsync(post.Id, "newsletter1", "all");

        Assert.Equal("Custom Subject Line", email.Subject);
    }

    [Fact]
    public async Task SendAsEmailAsync_NoHtml_Throws()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Empty" }, AuthorId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SendAsEmailAsync(post.Id, "newsletter1", "all"));
    }

    // ── DeleteAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingPost_Removes()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Test" }, AuthorId);

        await _sut.DeleteAsync(post.Id);

        Assert.Empty(_postRepo.Posts);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.DeleteAsync("nonexistent"));
    }

    // ── GetRevisionsAsync ───────────────────────────────────────

    [Fact]
    public async Task GetRevisionsAsync_ReturnsDescendingOrder()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", Lexical = "{}" },
            AuthorId);

        // Update to create a second revision
        await _sut.UpdateAsync(post.Id, new ContentUpdateRequest { Lexical = "{\"v2\":true}" }, AuthorId);

        var revisions = await _sut.GetRevisionsAsync(post.Id);

        Assert.Equal(2, revisions.Count);
        Assert.True(revisions[0].CreatedAt >= revisions[1].CreatedAt);
        Assert.Equal("edited", revisions[0].Reason);
        Assert.Equal("initial", revisions[1].Reason);
    }

    [Fact]
    public async Task RestoreRevisionAsync_RestoresContentAndCreatesRevision()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Original", Lexical = "{\"v1\":true}" },
            AuthorId);

        // Update to create a second revision
        await _sut.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "Updated", Lexical = "{\"v2\":true}" }, AuthorId);

        // Get revisions — initial revision is the one we want to restore
        var revisions = await _sut.GetRevisionsAsync(post.Id);
        var initialRevision = revisions.First(r => r.Reason == "initial");

        // Restore the initial revision
        var restored = await _sut.RestoreRevisionAsync(post.Id, initialRevision.Id, AuthorId);

        Assert.Equal("Original", restored.Title);
        Assert.Equal("{\"v1\":true}", restored.Lexical);

        // Verify a "restored" revision was created
        var revisionsAfter = await _sut.GetRevisionsAsync(post.Id);
        Assert.Equal("restored", revisionsAfter[0].Reason);
    }

    [Fact]
    public async Task RestoreRevisionAsync_ThrowsForInvalidRevisionId()
    {
        var post = await _sut.CreateAsync(
            new ContentCreateRequest { Title = "Test", Lexical = "{}" },
            AuthorId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RestoreRevisionAsync(post.Id, "nonexistent", AuthorId));
    }

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakePostRepository : IPostRepository
    {
        public List<Post> Posts { get; } = [];

        public Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));

        public Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Slug == slug));

        public Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult<Post?>(null);

        public Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));

        public Task AddAsync(Post post, CancellationToken ct = default)
        {
            Posts.Add(post);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Post post, CancellationToken ct = default)
            => Task.CompletedTask; // In-memory — already mutated

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Posts.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }

        // Unused by ContentService — minimal stubs
        public Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
        { Posts.RemoveAll(p => ids.Contains(p.Id)); return Task.CompletedTask; }
        public Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
        { foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured; return Task.CompletedTask; }
        public Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeLexicalRenderer : ILexicalRenderer
    {
        public string Render(string lexicalJson) => $"[lexical:{lexicalJson}]";
    }

    private sealed class FakeMobiledocRenderer : IMobiledocRenderer
    {
        public string Render(string mobiledocJson) => $"[mobiledoc:{mobiledocJson}]";
    }

    private sealed class FakeWebhookDispatch : IWebhookDispatchService
    {
        public List<(string Event, object Payload)> Enqueued { get; } = [];
        public void Enqueue(string eventName, object payload) => Enqueued.Add((eventName, payload));
    }

    private sealed class FakeIndexNowService : IIndexNowService
    {
        public List<string> EnqueuedUrls { get; } = [];
        public void Enqueue(string url) => EnqueuedUrls.Add(url);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
