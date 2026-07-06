using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IActivityPubService
{
    // Account discovery
    Task<ApAccount?> GetLocalActorAsync(CancellationToken ct = default);
    Task<WebFingerResult?> HandleWebFingerAsync(string resource, CancellationToken ct = default);

    // Follow/Unfollow
    Task HandleFollowAsync(string actorApId, string objectApId, CancellationToken ct = default);
    Task HandleUndoFollowAsync(string actorApId, string objectApId, CancellationToken ct = default);
    Task<PagedResult<ApAccount>> GetFollowersAsync(int accountId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<ApAccount>> GetFollowingAsync(int accountId, int page, int pageSize, CancellationToken ct = default);

    // Inbox processing
    Task ProcessInboxAsync(string body, string? signature, string? signatureInput, string host, CancellationToken ct = default);

    // Outbox / publish to fediverse
    Task<ApPost> PublishPostAsync(string ghostPostUuid, string title, string content, string url, string? imageUrl, CancellationToken ct = default);
    Task DeletePostAsync(string ghostPostUuid, CancellationToken ct = default);

    // Outbox collection
    Task<ActivityPubCollection> GetOutboxAsync(string username, int page, int pageSize, CancellationToken ct = default);

    // Remote account resolution
    Task<ApAccount?> ResolveRemoteAccountAsync(string apId, CancellationToken ct = default);
}

public class WebFingerResult
{
    public string Subject { get; set; } = null!;
    public List<WebFingerLink> Links { get; set; } = [];
}

public class WebFingerLink
{
    public string Rel { get; set; } = null!;
    public string? Type { get; set; }
    public string? Href { get; set; }
}

public class ActivityPubCollection
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = "OrderedCollection";
    public int TotalItems { get; set; }
    public string? First { get; set; }
    public string? Last { get; set; }
    public List<object>? OrderedItems { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
