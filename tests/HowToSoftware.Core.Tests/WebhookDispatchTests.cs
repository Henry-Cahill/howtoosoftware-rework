using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class WebhookDispatchTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly SlugGenerator _slugGenerator = new();
    private readonly FakeLexicalRenderer _lexicalRenderer = new();
    private readonly FakeMobiledocRenderer _mobiledocRenderer = new();
    private readonly RecordingWebhookDispatch _webhookDispatch = new();
    private readonly FakeIndexNowService _indexNow = new();
    private readonly ContentService _contentService;

    private const string AuthorId = "aaaaaaaaaaaaaaaaaaaaaaaa";

    public WebhookDispatchTests()
    {
        _contentService = new ContentService(
            _postRepo, _slugGenerator, _lexicalRenderer, _mobiledocRenderer, _webhookDispatch, _indexNow);
    }

    // ── post.added ──────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Post_EnqueuesPostAdded()
    {
        var request = new ContentCreateRequest { Title = "Test Post", Type = "post" };

        await _contentService.CreateAsync(request, AuthorId);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("post.added", _webhookDispatch.Events[0].EventName);
    }

    [Fact]
    public async Task CreateAsync_Page_EnqueuesPageAdded()
    {
        var request = new ContentCreateRequest { Title = "About", Type = "page" };

        await _contentService.CreateAsync(request, AuthorId);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("page.added", _webhookDispatch.Events[0].EventName);
    }

    // ── post.edited ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_Post_EnqueuesPostEdited()
    {
        var post = await CreateTestPost("post");

        await _contentService.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "Updated" }, AuthorId);

        var editEvent = _webhookDispatch.Events.Last();
        Assert.Equal("post.edited", editEvent.EventName);
    }

    [Fact]
    public async Task UpdateAsync_Page_EnqueuesPageEdited()
    {
        var post = await CreateTestPost("page");

        await _contentService.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "Updated" }, AuthorId);

        var editEvent = _webhookDispatch.Events.Last();
        Assert.Equal("page.edited", editEvent.EventName);
    }

    // ── post.published ──────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_Post_EnqueuesPostPublished()
    {
        var post = await CreateTestPost("post");

        await _contentService.PublishAsync(post.Id, AuthorId);

        var publishEvent = _webhookDispatch.Events.Last();
        Assert.Equal("post.published", publishEvent.EventName);
    }

    [Fact]
    public async Task PublishAsync_Page_EnqueuesPagePublished()
    {
        var post = await CreateTestPost("page");

        await _contentService.PublishAsync(post.Id, AuthorId);

        var publishEvent = _webhookDispatch.Events.Last();
        Assert.Equal("page.published", publishEvent.EventName);
    }

    [Fact]
    public async Task PublishAsync_AlreadyPublished_DoesNotEnqueueAgain()
    {
        var post = await CreateTestPost("post");
        await _contentService.PublishAsync(post.Id, AuthorId);
        var countAfterFirstPublish = _webhookDispatch.Events.Count;

        await _contentService.PublishAsync(post.Id, AuthorId);

        Assert.Equal(countAfterFirstPublish, _webhookDispatch.Events.Count);
    }

    // ── post.unpublished ────────────────────────────────────────

    [Fact]
    public async Task UnpublishAsync_Post_EnqueuesPostUnpublished()
    {
        var post = await CreateTestPost("post");
        await _contentService.PublishAsync(post.Id, AuthorId);
        _webhookDispatch.Events.Clear();

        await _contentService.UnpublishAsync(post.Id);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("post.unpublished", _webhookDispatch.Events[0].EventName);
    }

    [Fact]
    public async Task UnpublishAsync_AlreadyDraft_DoesNotEnqueue()
    {
        var post = await CreateTestPost("post");
        _webhookDispatch.Events.Clear();

        await _contentService.UnpublishAsync(post.Id);

        Assert.Empty(_webhookDispatch.Events);
    }

    // ── post.scheduled ──────────────────────────────────────────

    [Fact]
    public async Task ScheduleAsync_EnqueuesPostScheduled()
    {
        var post = await CreateTestPost("post");
        _webhookDispatch.Events.Clear();

        await _contentService.ScheduleAsync(post.Id, DateTime.UtcNow.AddDays(1), AuthorId);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("post.scheduled", _webhookDispatch.Events[0].EventName);
    }

    // ── post.deleted ────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Post_EnqueuesPostDeleted()
    {
        var post = await CreateTestPost("post");
        _webhookDispatch.Events.Clear();

        await _contentService.DeleteAsync(post.Id);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("post.deleted", _webhookDispatch.Events[0].EventName);
    }

    [Fact]
    public async Task DeleteAsync_Page_EnqueuesPageDeleted()
    {
        var post = await CreateTestPost("page");
        _webhookDispatch.Events.Clear();

        await _contentService.DeleteAsync(post.Id);

        Assert.Single(_webhookDispatch.Events);
        Assert.Equal("page.deleted", _webhookDispatch.Events[0].EventName);
    }

    // ── helpers ─────────────────────────────────────────────────

    private async Task<Post> CreateTestPost(string type)
    {
        var request = new ContentCreateRequest { Title = $"Test {type}", Type = type };
        return await _contentService.CreateAsync(request, AuthorId);
    }

    // ── fakes ───────────────────────────────────────────────────

    private sealed class RecordingWebhookDispatch : IWebhookDispatchService
    {
        public List<(string EventName, object Payload)> Events { get; } = [];
        public void Enqueue(string eventName, object payload) => Events.Add((eventName, payload));
    }

    private sealed class FakePostRepository : IPostRepository
    {
        private readonly List<Post> Posts = [];

        public Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));
        public Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Slug == slug));
        public Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult<Post?>(null);
        public Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));
        public Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Post> { Items = [], Page = 1, PageSize = pageSize, TotalCount = 0 });
        public Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Post> { Items = [], Page = 1, PageSize = pageSize, TotalCount = 0 });
        public Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Post> { Items = [], Page = 1, PageSize = pageSize, TotalCount = 0 });
        public Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Post> { Items = [], Page = 1, PageSize = pageSize, TotalCount = 0 });
        public Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<Post> { Items = Posts.ToList(), Page = 1, PageSize = pageSize, TotalCount = Posts.Count });
        public Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
            => Task.FromResult(new List<Post>());
        public Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
            => Task.FromResult(new List<Post>());
        public Task AddAsync(Post post, CancellationToken ct = default)
            { Posts.Add(post); return Task.CompletedTask; }
        public Task UpdateAsync(Post post, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
            { Posts.RemoveAll(p => p.Id == id); return Task.CompletedTask; }
        public Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
            { Posts.RemoveAll(p => ids.Contains(p.Id)); return Task.CompletedTask; }
        public Task<List<Post>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
            => Task.FromResult(Posts.Where(p => ids.Contains(p.Id)).ToList());
        public Task BulkSetFeaturedAsync(IEnumerable<string> ids, bool featured, CancellationToken ct = default)
            { foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured; return Task.CompletedTask; }
        public Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
            { foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured; return Task.CompletedTask; }
        public Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
            => Task.FromResult(Posts.Count);
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
