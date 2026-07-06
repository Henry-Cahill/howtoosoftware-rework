using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class IndexNowServiceTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly SlugGenerator _slugGenerator = new();
    private readonly FakeLexicalRenderer _lexicalRenderer = new();
    private readonly FakeMobiledocRenderer _mobiledocRenderer = new();
    private readonly FakeWebhookDispatch _webhookDispatch = new();
    private readonly FakeIndexNowService _indexNow = new();
    private readonly ContentService _sut;

    private const string AuthorId = "aaaaaaaaaaaaaaaaaaaaaaaa";
    private const string PublisherId = "bbbbbbbbbbbbbbbbbbbbbbbb";

    public IndexNowServiceTests()
    {
        _sut = new ContentService(_postRepo, _slugGenerator, _lexicalRenderer, _mobiledocRenderer, _webhookDispatch, _indexNow);
    }

    // ── PublishAsync ────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_EnqueuesIndexNowUrl()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "IndexNow Test" }, AuthorId);

        await _sut.PublishAsync(post.Id, PublisherId);

        Assert.Single(_indexNow.EnqueuedUrls);
        Assert.Equal("/indexnow-test/", _indexNow.EnqueuedUrls[0]);
    }

    [Fact]
    public async Task PublishAsync_AlreadyPublished_DoesNotEnqueueIndexNow()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Already Published" }, AuthorId);
        await _sut.PublishAsync(post.Id, PublisherId);
        _indexNow.EnqueuedUrls.Clear();

        // Second publish should be a no-op
        await _sut.PublishAsync(post.Id, PublisherId);

        Assert.Empty(_indexNow.EnqueuedUrls);
    }

    [Fact]
    public async Task PublishAsync_Page_EnqueuesIndexNowUrl()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "About", Type = "page" }, AuthorId);

        await _sut.PublishAsync(post.Id, PublisherId);

        Assert.Single(_indexNow.EnqueuedUrls);
        Assert.Equal("/about/", _indexNow.EnqueuedUrls[0]);
    }

    // ── UpdateAsync (published) ─────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PublishedPost_EnqueuesIndexNow()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Live Post" }, AuthorId);
        await _sut.PublishAsync(post.Id, PublisherId);
        _indexNow.EnqueuedUrls.Clear();

        await _sut.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "Updated Live Post" }, AuthorId);

        Assert.Single(_indexNow.EnqueuedUrls);
        Assert.Equal("/live-post/", _indexNow.EnqueuedUrls[0]);
    }

    [Fact]
    public async Task UpdateAsync_DraftPost_DoesNotEnqueueIndexNow()
    {
        var post = await _sut.CreateAsync(new ContentCreateRequest { Title = "Draft Post" }, AuthorId);

        await _sut.UpdateAsync(post.Id, new ContentUpdateRequest { Title = "Still a Draft" }, AuthorId);

        Assert.Empty(_indexNow.EnqueuedUrls);
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
            => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Posts.RemoveAll(p => p.Id == id);
            return Task.CompletedTask;
        }

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
